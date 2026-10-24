using Demolite.Core.Models;
using Innkeep2.Models.Internal;
using Innkeep2.Models.Internal.Receipt;
using Innkeep2.Models.Shared;
using Innkeep2.Requests.Server;

namespace Innkeep2.Services.Client;

public enum CardPaymentPhase
{
    NotStarted,
    Starting,
    Pending,
    Completed,
    Failed,
    GaveUp
}

public sealed class CardPaymentSession(
    IServerPaymentClient client,
    ClientCartService cart,
    ICompletedOrderRegistry completedOrders,
    ClientCardPaymentOptions options,
    TimeProvider timeProvider,
    Func<TimeSpan, CancellationToken, Task> delay)
{
    private const int NotFoundLimit = 2;
    private const string CloudUnreachableCode = "Payment.CloudUnreachable";
    private const string FailedCode = "Payment.Failed";
    private const string NotFoundCode = "Payment.NotFound";

    private Guid? _requestId;
    private DateTime _startedAt;
    private PaymentRequestStatus? _status;
    private bool _startUncertain;
    private int _notFoundCount;

    public CardPaymentPhase Phase { get; private set; } = CardPaymentPhase.NotStarted;

    public string StatusText { get; private set; } = "Bereit zur Zahlung";

    public TransactionReceipt? Receipt { get; private set; }

    public decimal Amount => cart.Total;

    public bool CanStart => Phase is CardPaymentPhase.NotStarted or CardPaymentPhase.Failed;

    public bool CanSwitchToCash => CanStart;

    public bool CanCancel => Phase == CardPaymentPhase.Pending && !_startUncertain && _status == PaymentRequestStatus.Pending;

    public bool IsFinished => Phase is CardPaymentPhase.Completed or CardPaymentPhase.GaveUp;

    public event EventHandler? Changed;

    private DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    /// <summary>
    /// Starts a new payment attempt and follows it until it has a final status or is given up.
    /// </summary>
    public async Task StartAsync(CancellationToken ct = default)
    {
        if (!CanStart)
            return;

        var order = BuildOrder();

        _requestId = order.RequestId;
        _startedAt = UtcNow;
        _status = null;
        _startUncertain = false;
        _notFoundCount = 0;
        Update(CardPaymentPhase.Starting, "Zahlung wird gestartet ...");

        var started = await client.StartCardPaymentAsync(order, ct);

        if (started.IsSuccess)
            Apply(started.Value!);
        else
            HandleStartFailure(started.Error!);

        if (Phase == CardPaymentPhase.Pending)
            await PollAsync(ct);
    }

    /// <summary>
    /// Asks the reader to stop a pending payment. The payment ends once SumUp confirms the outcome.
    /// </summary>
    public async Task CancelAsync(CancellationToken ct = default)
    {
        if (!CanCancel)
            return;

        var cancelled = await client.CancelCardPaymentAsync(_requestId!.Value, ct);

        if (cancelled.IsSuccess)
            Apply(cancelled.Value!);
        else
            Update(Phase, "Der Abbruch konnte nicht gesendet werden");
    }

    private OrderRequest BuildOrder() => new()
    {
        RequestId = Guid.NewGuid(),
        Items = cart.Items.ToList(),
        PaymentType = PaymentType.NonCash,
        AmountGiven = cart.Total,
        Currency = "EUR"
    };

    private async Task PollAsync(CancellationToken ct)
    {
        while (Phase == CardPaymentPhase.Pending)
        {
            if (UtcNow - _startedAt >= options.GiveUpAfter)
            {
                Update(CardPaymentPhase.GaveUp, "Die Zahlung konnte nicht bestätigt werden. Bitte in der SumUp Cloud prüfen, ob die Zahlung erfolgreich war.");
                return;
            }

            await delay(options.PollInterval, ct);

            var polled = await client.GetCardPaymentAsync(_requestId!.Value, ct);

            if (polled.IsSuccess)
            {
                _notFoundCount = 0;
                Apply(polled.Value!);
            }
            else
            {
                HandlePollFailure(polled.Error!);
            }
        }
    }

    private void HandleStartFailure(Error error)
    {
        if (IsCleanRefusal(error))
        {
            Update(CardPaymentPhase.Failed, error.Message);
            return;
        }

        _startUncertain = true;
        Update(CardPaymentPhase.Pending, "Verbindung gestört, es wird geprüft ob die Zahlung gestartet wurde ...");
    }

    private void HandlePollFailure(Error error)
    {
        if (error.Code == NotFoundCode && _startUncertain && ++_notFoundCount >= NotFoundLimit)
        {
            Update(CardPaymentPhase.Failed, "Die Zahlung wurde nicht gestartet, bitte erneut versuchen");
            return;
        }

        Update(Phase, error.Code == CloudUnreachableCode
            ? "Verbindung zur Cloud gestört, warte auf die Bestätigung ..."
            : "Der Status der Zahlung kann nicht abgefragt werden, warte auf die Bestätigung ...");
    }

    private void Apply(PaymentStatusResponse response)
    {
        _status = response.Status;
        _startUncertain = false;

        switch (response.Status)
        {
            case PaymentRequestStatus.Completed when response.Receipt is null:
                Update(CardPaymentPhase.Pending, "Zahlung abgeschlossen, der Beleg wird geladen ...");
                break;
            case PaymentRequestStatus.Completed:
                Receipt = response.Receipt;
                completedOrders.RegisterCompletedOrder(response.Receipt!);
                Update(CardPaymentPhase.Completed, "Zahlung erfolgreich");
                break;
            case PaymentRequestStatus.Failed or PaymentRequestStatus.Cancelled or PaymentRequestStatus.Expired:
                Update(CardPaymentPhase.Failed, response.Message ?? "Die Zahlung ist fehlgeschlagen");
                break;
            default:
                Update(CardPaymentPhase.Pending, response.Message ?? "Bitte am Kartenleser bezahlen");
                break;
        }
    }

    private static bool IsCleanRefusal(Error error)
        => error.Code.StartsWith("Payment.") && error.Code is not CloudUnreachableCode and not FailedCode;

    private void Update(CardPaymentPhase phase, string statusText)
    {
        Phase = phase;
        StatusText = statusText;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
