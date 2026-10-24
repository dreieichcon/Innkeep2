using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Demolite.Core.Models;
using Innkeep2.Cloud.Services.Transactions;
using Innkeep2.Cloud.TransactionDb.Models;
using Innkeep2.Cloud.TransactionDb.Repositories;
using Innkeep2.CredentialsDb;
using Innkeep2.CredentialsDb.Models;
using Innkeep2.Models.Internal;
using Innkeep2.Models.Shared;
using Innkeep2.Models.SumUp;
using Innkeep2.Requests.SumUp;
using Serilog;

namespace Innkeep2.Cloud.Services.Payments;

public enum SumUpWebhookResult
{
    Unauthorized,
    Ignored,
    Processed
}

public sealed class CardPaymentService(
    ISumUpClient sumUp,
    PaymentRequestRepository payments,
    IOrderCreationService orders,
    ActiveCredentialProvider<SumUpCredential> accountCredentials,
    ActiveCredentialProvider<SumUpReaderCredential> readerCredentials,
    CardPaymentOptions options,
    TimeProvider timeProvider)
{
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();
    private readonly ConcurrentDictionary<Guid, DateTime> _lastVerified = new();

    private DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    /// <summary>
    /// Starts a card payment on the active reader. A request id that was started before returns its current status.
    /// </summary>
    public async Task<Result<PaymentStatusResponse>> StartAsync(OrderRequest request, CancellationToken ct = default)
    {
        if (Validate(request) is { } invalid)
            return Result<PaymentStatusResponse>.Failure(invalid);

        if ((await payments.GetByRequestIdAsync(request.RequestId, ct)).IsSuccess)
            return await GetStatusAsync(request.RequestId, ct);

        var target = ResolveTarget();

        if (!target.IsSuccess)
            return Result<PaymentStatusResponse>.Failure(target.Error!);

        var busy = await payments.HasPendingAsync(CardPaymentRules.ReaderBusyCutoff(UtcNow, options), ct);

        if (busy is { IsSuccess: true, Value: true })
            return Result<PaymentStatusResponse>.Failure(new Error("Payment.ReaderBusy", "Der Kartenleser ist gerade in Benutzung"));

        var created = await payments.CreateAsync(BuildPaymentRequest(request, target.Value!.Reader), ct);

        if (!created.IsSuccess)
            return Result<PaymentStatusResponse>.Failure(new Error("Payment.StoreFailed", "Die Zahlung konnte nicht gespeichert werden"));

        var payment = created.Value!;
        var checkout = await sumUp.CreateCheckoutAsync(payment.MerchantCode, payment.ReaderId, BuildCheckout(request, target.Value.Account), ct);

        if (checkout.IsSuccess)
            await StoreCheckoutAsync(payment, checkout.Value!.Data, ct);
        else
            await FailStartAsync(payment, checkout.Error!, ct);

        return Result<PaymentStatusResponse>.Success(await BuildResponseAsync(payment, null, ct));
    }

    /// <summary>
    /// Returns the status of a payment and moves it forward when SumUp has an outcome.
    /// </summary>
    public async Task<Result<PaymentStatusResponse>> GetStatusAsync(Guid requestId, CancellationToken ct = default)
    {
        var payment = await UnderLockAsync(requestId, p => ReconcileAsync(p, false, ct), ct);

        return payment is null
            ? NotFound()
            : Result<PaymentStatusResponse>.Success(await BuildResponseAsync(payment, null, ct));
    }

    /// <summary>
    /// Asks the reader to cancel a pending payment. The payment ends once SumUp confirms the outcome.
    /// </summary>
    public async Task<Result<PaymentStatusResponse>> CancelAsync(Guid requestId, CancellationToken ct = default)
    {
        var cancelFailed = false;
        var payment = await UnderLockAsync(requestId, async p => cancelFailed = !await CancelPendingAsync(p, ct), ct);

        return payment is null
            ? NotFound()
            : Result<PaymentStatusResponse>.Success(await BuildResponseAsync(payment, cancelFailed ? "Der Abbruch ist am Kartenleser nicht mehr möglich" : null, ct));
    }

    /// <summary>
    /// Handles a SumUp callback. The callback only triggers a check, the outcome is always read from SumUp.
    /// </summary>
    public async Task<SumUpWebhookResult> HandleWebhookAsync(string? token, SumUpWebhookEvent webhook, CancellationToken ct = default)
    {
        if (accountCredentials.Active is not { } account || !TokenMatches(account.WebhookToken, token))
            return SumUpWebhookResult.Unauthorized;

        if (webhook.EventType != SumUpWebhookEvent.SoloTransactionUpdated)
            return SumUpWebhookResult.Ignored;

        var found = await payments.GetByClientTransactionIdAsync(webhook.Payload.ClientTransactionId, ct);

        if (!found.IsSuccess)
            return SumUpWebhookResult.Ignored;

        await UnderLockAsync(found.Value!.RequestId, p => ReconcileAsync(p, true, ct), ct);

        return SumUpWebhookResult.Processed;
    }

    private async Task ReconcileAsync(PaymentRequest payment, bool forceVerify, CancellationToken ct)
    {
        if (payment.Status == PaymentRequestStatus.Pending)
            await ProcessPendingAsync(payment, forceVerify, ct);

        if (payment.Status == PaymentRequestStatus.Paid)
            await CompleteAsync(payment, ct);
    }

    private async Task ProcessPendingAsync(PaymentRequest payment, bool forceVerify, CancellationToken ct)
    {
        if (CardPaymentRules.ShouldTerminateForTimeout(payment, UtcNow, options))
            await TerminateForTimeoutAsync(payment, ct);

        DateTime? lastVerified = _lastVerified.TryGetValue(payment.RequestId, out var last) ? last : null;

        if (forceVerify || CardPaymentRules.ShouldVerify(payment, UtcNow, lastVerified, options))
            await VerifyAsync(payment, ct);
    }

    private async Task TerminateForTimeoutAsync(PaymentRequest payment, CancellationToken ct)
    {
        var terminated = await sumUp.TerminateAsync(payment.MerchantCode, payment.ReaderId, ct);

        if (!terminated.IsSuccess)
            Log.Warning("Terminating the timed out payment {RequestId} failed: {Error}", payment.RequestId, terminated.Error!.Message);

        payment.TerminationReason = PaymentTerminationReason.Timeout;
        await SaveAsync(payment, ct);
    }

    private async Task<bool> CancelPendingAsync(PaymentRequest payment, CancellationToken ct)
    {
        if (payment.Status != PaymentRequestStatus.Pending || payment.TerminationReason is not null)
            return true;

        var terminated = await sumUp.TerminateAsync(payment.MerchantCode, payment.ReaderId, ct);

        if (!terminated.IsSuccess)
        {
            Log.Warning("Cancelling payment {RequestId} failed: {Error}", payment.RequestId, terminated.Error!.Message);
            return false;
        }

        payment.TerminationReason = PaymentTerminationReason.UserCancelled;
        await SaveAsync(payment, ct);
        await ReconcileAsync(payment, true, ct);

        return true;
    }

    private async Task VerifyAsync(PaymentRequest payment, CancellationToken ct)
    {
        _lastVerified[payment.RequestId] = UtcNow;

        if (payment.ClientTransactionId is null)
            return;

        var lookup = await sumUp.GetTransactionAsync(payment.MerchantCode, payment.ClientTransactionId, ct);

        if (!lookup.IsSuccess)
        {
            Log.Warning("Looking up the transaction of payment {RequestId} failed: {Error}", payment.RequestId, lookup.Error!.Message);
            return;
        }

        await ApplyOutcomeAsync(payment, lookup.Value!, ct);
    }

    private async Task ApplyOutcomeAsync(PaymentRequest payment, SumUpTransaction transaction, CancellationToken ct)
    {
        switch (CardPaymentRules.MapOutcome(transaction.Status))
        {
            case SumUpOutcome.Succeeded when !MatchesPayment(payment, transaction):
                Log.Error("SumUp reports a different amount for payment {RequestId}: {Amount} {Currency}", payment.RequestId, transaction.Amount, transaction.Currency);
                break;
            case SumUpOutcome.Succeeded:
                await TransitionAsync(payment, PaymentRequestStatus.Paid, p => p.SumupTransactionCode = transaction.TransactionCode, ct);
                break;
            case SumUpOutcome.Failed:
                await TransitionAsync(payment, CardPaymentRules.ResolveFailedStatus(payment.TerminationReason), p => p.FailureReason = $"SumUp status {transaction.Status}", ct);
                break;
            case SumUpOutcome.Unknown:
                Log.Warning("SumUp reports the unhandled status {Status} for payment {RequestId}", transaction.Status, payment.RequestId);
                break;
        }
    }

    private async Task CompleteAsync(PaymentRequest payment, CancellationToken ct)
    {
        var order = DeserializeOrder(payment);

        if (order is null)
            return;

        var created = await orders.CreateOrderAsync(order, ct, isRetry: true, payment.SumupTransactionCode);

        if (!created.IsSuccess)
        {
            Log.Error("Creating the order of the paid payment {RequestId} failed: {Error}", payment.RequestId, created.Error!.Message);
            return;
        }

        await TransitionAsync(payment, PaymentRequestStatus.Completed, null, ct);
    }

    private static OrderRequest? DeserializeOrder(PaymentRequest payment)
    {
        try
        {
            return JsonSerializer.Deserialize<OrderRequest>(payment.OrderRequestJson);
        }
        catch (JsonException ex)
        {
            Log.Error(ex, "The stored order of the paid payment {RequestId} cannot be read", payment.RequestId);
            return null;
        }
    }

    private async Task<bool> TransitionAsync(PaymentRequest payment, PaymentRequestStatus to, Action<PaymentRequest>? change, CancellationToken ct)
    {
        var from = payment.Status;

        if (!CardPaymentRules.CanTransition(from, to))
        {
            Log.Warning("Payment {RequestId} cannot change from {From} to {To}", payment.RequestId, from, to);
            return false;
        }

        payment.Status = to;
        change?.Invoke(payment);

        if (await SaveAsync(payment, ct))
            return true;

        payment.Status = from;
        return false;
    }

    private async Task<bool> SaveAsync(PaymentRequest payment, CancellationToken ct)
    {
        payment.UpdatedAt = UtcNow;
        var saved = await payments.UpdateAsync(payment, ct);

        if (!saved.IsSuccess)
            Log.Error("Saving payment {RequestId} failed: {Error}", payment.RequestId, saved.Error!.Message);

        return saved.IsSuccess;
    }

    private async Task StoreCheckoutAsync(PaymentRequest payment, SumUpCheckoutData checkout, CancellationToken ct)
    {
        payment.ClientTransactionId = checkout.ClientTransactionId;
        payment.CheckoutId = checkout.CheckoutId;
        await SaveAsync(payment, ct);
    }

    private async Task FailStartAsync(PaymentRequest payment, Error error, CancellationToken ct)
    {
        Log.Warning("Starting the checkout of payment {RequestId} failed: {Code} {Message}", payment.RequestId, error.Code, error.Message);

        await TransitionAsync(payment, PaymentRequestStatus.Failed, p => p.FailureReason = $"{error.Code}: {error.Message}", ct);
    }

    private async Task<PaymentRequest?> UnderLockAsync(Guid requestId, Func<PaymentRequest, Task> action, CancellationToken ct)
    {
        var gate = _locks.GetOrAdd(requestId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);

        try
        {
            var loaded = await payments.GetByRequestIdAsync(requestId, ct);

            if (!loaded.IsSuccess)
                return null;

            await action(loaded.Value!);

            return loaded.Value;
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<PaymentStatusResponse> BuildResponseAsync(PaymentRequest payment, string? message, CancellationToken ct)
    {
        var receipt = payment.Status == PaymentRequestStatus.Completed
            ? await orders.GetReceiptAsync(payment.RequestId, ct)
            : null;

        return new PaymentStatusResponse(payment.RequestId, payment.Status, message ?? Describe(payment), receipt);
    }

    private static string? Describe(PaymentRequest payment)
        => payment.Status switch
        {
            PaymentRequestStatus.Pending when payment.TerminationReason is not null => "Warte auf die Bestätigung von SumUp",
            PaymentRequestStatus.Pending => "Bitte am Kartenleser bezahlen",
            PaymentRequestStatus.Paid => "Zahlung erhalten, die Bestellung wird angelegt",
            PaymentRequestStatus.Failed => "Die Zahlung ist fehlgeschlagen",
            PaymentRequestStatus.Cancelled => "Die Zahlung wurde abgebrochen",
            PaymentRequestStatus.Expired => "Zeitüberschreitung, die Zahlung wurde nicht abgeschlossen",
            _ => null
        };

    private static Error? Validate(OrderRequest request)
        => request switch
        {
            { PaymentType: not PaymentType.NonCash } => new Error("Payment.NotCard", "Nur Kartenzahlungen können gestartet werden"),
            { Items.Count: 0 } => new Error("Payment.NoItems", "Die Bestellung enthält keine Artikel"),
            { AmountNeeded: <= 0 } => new Error("Payment.NoAmount", "Der Betrag muss größer als 0 sein"),
            _ when request.AmountGiven != request.AmountNeeded => new Error("Payment.AmountMismatch", "Bei Kartenzahlung müssen Summe und gegebener Betrag übereinstimmen"),
            _ => null
        };

    private Result<PaymentTarget> ResolveTarget()
        => (accountCredentials.Active, readerCredentials.Active) switch
        {
            (null, _) => Result<PaymentTarget>.Failure(new Error("Payment.NoAccount", "Es sind keine aktiven SumUp-Zugangsdaten hinterlegt")),
            (_, null) => Result<PaymentTarget>.Failure(new Error("Payment.NoReader", "Es ist kein aktiver SumUp-Reader hinterlegt")),
            ({ } account, { } reader) when account.MerchantCode != reader.MerchantCode
                => Result<PaymentTarget>.Failure(new Error("Payment.MerchantMismatch", "Der aktive Reader gehört zu einem anderen Händlercode als die aktiven SumUp-Zugangsdaten")),
            ({ } account, { } reader) => Result<PaymentTarget>.Success(new PaymentTarget(account, reader))
        };

    private PaymentRequest BuildPaymentRequest(OrderRequest request, SumUpReaderCredential reader) => new()
    {
        RequestId = request.RequestId,
        OrderRequestJson = JsonSerializer.Serialize(request),
        TotalAmount = request.AmountNeeded,
        Currency = request.Currency,
        MerchantCode = reader.MerchantCode,
        ReaderId = reader.ReaderId,
        CreatedAt = UtcNow,
        UpdatedAt = UtcNow
    };

    private static SumUpCheckoutRequest BuildCheckout(OrderRequest request, SumUpCredential account) => new()
    {
        TotalAmount = SumUpMoney.FromDecimal(request.AmountNeeded, request.Currency),
        ReturnUrl = $"{account.PublicBaseUrl.TrimEnd('/')}/webhooks/sumup/{account.WebhookToken}",
        Affiliate = BuildAffiliate(account, request.RequestId)
    };

    private static SumUpAffiliate? BuildAffiliate(SumUpCredential account, Guid requestId)
        => string.IsNullOrWhiteSpace(account.AffiliateAppId) || string.IsNullOrWhiteSpace(account.AffiliateKey)
            ? null
            : new SumUpAffiliate { AppId = account.AffiliateAppId, Key = account.AffiliateKey, ForeignTransactionId = requestId.ToString() };

    private static bool MatchesPayment(PaymentRequest payment, SumUpTransaction transaction)
        => (transaction.Amount is null || transaction.Amount == payment.TotalAmount)
           && (transaction.Currency is null || string.Equals(transaction.Currency, payment.Currency, StringComparison.OrdinalIgnoreCase));

    private static bool TokenMatches(string expected, string? actual)
        => actual is not null
           && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(actual));

    private static Result<PaymentStatusResponse> NotFound()
        => Result<PaymentStatusResponse>.Failure(new Error("Payment.NotFound", "Die Zahlung wurde nicht gefunden"));

    private sealed record PaymentTarget(SumUpCredential Account, SumUpReaderCredential Reader);
}
