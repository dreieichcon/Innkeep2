using System.Collections.Concurrent;
using Demolite.Core.Models;
using Innkeep2.Cloud.TransactionDb.Repositories;
using Innkeep2.Models.SumUp;
using Innkeep2.Requests.SumUp;
using Serilog;

namespace Innkeep2.Cloud.Services.Payments;

public interface ICardRefundService
{
    Task<Result> RefundAsync(Guid originalRequestId, CancellationToken ct = default);
}

public static class CardRefundErrors
{
    public const string PaymentMissing = "Refund.CardPaymentMissing";
    public const string NotRefundable = "Refund.CardNotRefundable";
    public const string Rejected = "Refund.CardRefundRejected";
    public const string NotPermitted = "Refund.CardNotPermitted";
    public const string Unavailable = "Refund.CardUnavailable";
}

public sealed class CardRefundService(ISumUpClient sumUp, PaymentRequestRepository payments) : ICardRefundService
{
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();

    /// <summary>
    /// Refunds the card payment of the order at SumUp. A payment that SumUp already shows as refunded counts as done.
    /// </summary>
    public async Task<Result> RefundAsync(Guid originalRequestId, CancellationToken ct = default)
    {
        var gate = _locks.GetOrAdd(originalRequestId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);

        try
        {
            return await RefundLockedAsync(originalRequestId, ct);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<Result> RefundLockedAsync(Guid originalRequestId, CancellationToken ct)
    {
        var stored = await payments.GetByRequestIdAsync(originalRequestId, ct);

        if (!stored.IsSuccess || string.IsNullOrEmpty(stored.Value!.ClientTransactionId))
            return Fail(CardRefundErrors.PaymentMissing, "Zu dieser Bestellung wurde keine SumUp-Zahlung gefunden");

        var payment = stored.Value;
        var lookup = await sumUp.GetTransactionAsync(payment.MerchantCode, payment.ClientTransactionId!, ct);

        if (!lookup.IsSuccess)
            return Fail(MapLookupError(lookup.Error!));

        var transaction = lookup.Value!;

        if (string.IsNullOrWhiteSpace(transaction.Id))
            return Fail(CardRefundErrors.Unavailable, "SumUp hat keine Transaktions-ID geliefert, die Rückerstattung ist nicht möglich");

        var total = transaction.Amount ?? payment.TotalAmount;
        var refunded = transaction.RefundedAmount ?? 0m;

        if (IsStatus(transaction, "REFUNDED") || refunded >= total)
        {
            Log.Information("Payment {RequestId} is already refunded at SumUp", originalRequestId);
            return Result.Success();
        }

        if (!IsStatus(transaction, "SUCCESSFUL"))
            return Fail(CardRefundErrors.NotRefundable, "Die Zahlung ist bei SumUp nicht erfolgreich und kann nicht zurückerstattet werden");

        var amount = refunded > 0 ? total - refunded : (decimal?)null;
        var refund = await sumUp.RefundTransactionAsync(payment.MerchantCode, transaction.Id, amount, ct);

        return refund.IsSuccess ? Result.Success() : Fail(MapRefundError(refund.Error!));
    }

    private static Error MapLookupError(Error error)
        => error.Code switch
        {
            "Http.404" => Create(CardRefundErrors.PaymentMissing, "Die Zahlung wurde bei SumUp nicht gefunden"),
            "Http.401" or "Http.403" => Create(CardRefundErrors.NotPermitted, "Der SumUp API Key ist ungültig oder hat keine Berechtigung"),
            _ => Create(CardRefundErrors.Unavailable, "SumUp ist nicht erreichbar, bitte später erneut versuchen")
        };

    private static Error MapRefundError(Error error)
    {
        Log.Warning("The SumUp refund failed: {Code} {Message}", error.Code, error.Message);

        return error.Code switch
        {
            "Http.401" or "Http.403" => Create(CardRefundErrors.NotPermitted, "Der SumUp API Key hat keine Berechtigung für Rückerstattungen (refunds.write)"),
            "Http.404" => Create(CardRefundErrors.PaymentMissing, "Die Zahlung wurde bei SumUp nicht gefunden"),
            "Http.409" => Create(CardRefundErrors.NotRefundable, "SumUp erlaubt die Rückerstattung dieser Zahlung nicht mehr"),
            "Http.400" or "Http.422" => Create(CardRefundErrors.Rejected, "SumUp hat die Rückerstattung abgelehnt"),
            _ => Create(CardRefundErrors.Unavailable, "SumUp ist nicht erreichbar, bitte später erneut versuchen")
        };
    }

    private static bool IsStatus(SumUpTransaction transaction, string status)
        => string.Equals(transaction.Status, status, StringComparison.OrdinalIgnoreCase);

    private static Result Fail(string code, string message)
        => Result.Failure(Create(code, message));

    private static Result Fail(Error error)
        => Result.Failure(error);

    private static Error Create(string code, string message)
        => new(code, message);
}
