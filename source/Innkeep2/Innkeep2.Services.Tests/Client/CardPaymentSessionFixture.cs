using Demolite.Core.Models;
using Innkeep2.Models.Internal;
using Innkeep2.Models.Internal.Receipt;
using Innkeep2.Models.Shared;
using Innkeep2.Requests.Server;
using Innkeep2.Services.Client;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Innkeep2.Services.Tests.Client;

internal sealed class CardPaymentSessionFixture
{
    public CardPaymentSessionFixture(decimal price = 15m, ClientCardPaymentOptions? options = null)
    {
        Options = options ?? new ClientCardPaymentOptions();

        if (price > 0)
            Cart.AddItem(new SalesItem { Id = 1, VariationId = 0, Name = "Item", Price = price, TaxRate = 19m }, 1);

        Client.StartCardPaymentAsync(Arg.Any<OrderRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => Success(call.Arg<OrderRequest>().RequestId, PaymentRequestStatus.Pending, "Bitte am Kartenleser bezahlen"));
        Client.GetCardPaymentAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Success(Guid.Empty, PaymentRequestStatus.Pending, "Bitte am Kartenleser bezahlen"));

        Session = new CardPaymentSession(Client, Cart, Registry, Options, Time, WaitAsync);
    }

    public IServerPaymentClient Client { get; } = Substitute.For<IServerPaymentClient>();

    public ClientCartService Cart { get; } = new();

    public ICompletedOrderRegistry Registry { get; } = Substitute.For<ICompletedOrderRegistry>();

    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 10, 24, 12, 0, 0, TimeSpan.Zero));

    public ClientCardPaymentOptions Options { get; }

    public CardPaymentSession Session { get; }

    public List<TimeSpan> Waits { get; } = [];

    public int Changes { get; private set; }

    public static Result<PaymentStatusResponse> Success(Guid id, PaymentRequestStatus status, string? message = null, TransactionReceipt? receipt = null)
        => Result<PaymentStatusResponse>.Success(new PaymentStatusResponse(id, status, message, receipt));

    public static Result<PaymentStatusResponse> Failure(string code, string message = "message")
        => Result<PaymentStatusResponse>.Failure(new Error(code, message));

    public static TransactionReceipt BuildReceipt() => new()
    {
        OrderId = Guid.NewGuid(),
        TransactionType = TransactionType.Sale,
        Title = "Title",
        Header = "Header",
        BookingTime = DateTime.UtcNow,
        Currency = "EUR",
        Lines = [],
        Sum = new ReceiptSum { TotalAmount = 15m, AmountGiven = 15m, AmountReturned = 0m },
        TaxInformation = [],
        Vouchers = [],
        PaymentType = PaymentType.NonCash
    };

    /// <summary>
    /// Makes consecutive status requests return the given results; the last one repeats.
    /// </summary>
    public void StatusReturns(params Result<PaymentStatusResponse>[] results)
        => Client.GetCardPaymentAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(results[0], results.Skip(1).ToArray());

    public void StartReturns(Result<PaymentStatusResponse> result)
        => Client.StartCardPaymentAsync(Arg.Any<OrderRequest>(), Arg.Any<CancellationToken>()).Returns(result);

    public void TrackChanges()
        => Session.Changed += (_, _) => Changes++;

    private Task WaitAsync(TimeSpan time, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Waits.Add(time);
        Time.Advance(time);

        return Task.CompletedTask;
    }
}
