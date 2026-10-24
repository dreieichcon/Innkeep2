using Innkeep2.Models.Internal;
using Innkeep2.Models.Shared;
using Innkeep2.Requests.Server;
using Innkeep2.Services.Client;
using NSubstitute;

namespace Innkeep2.Services.Tests.Client;

[TestClass]
public class ClientCardPaymentServiceTests
{
    private readonly ClientCartService _cart = new();

    private ClientCardPaymentService CreateService() => new(
        Substitute.For<IServerPaymentClient>(),
        _cart,
        Substitute.For<ICompletedOrderRegistry>(),
        new ClientCardPaymentOptions(),
        TimeProvider.System);

    private static SalesItem Item(decimal price, int id = 1) => new() { Id = id, VariationId = 0, Name = "Item", Price = price, TaxRate = 19m };

    [TestMethod]
    public void CanPayByCard_IsFalseForAnEmptyCart()
        => Assert.IsFalse(CreateService().CanPayByCard);

    [TestMethod]
    public void CanPayByCard_IsFalseWhenTheTotalIsZero()
    {
        _cart.AddItem(Item(0m), 1);

        Assert.IsFalse(CreateService().CanPayByCard);
    }

    [TestMethod]
    public void CanPayByCard_IsTrueForACartWithAnAmountAboveZero()
    {
        _cart.AddItem(Item(0.01m), 1);

        Assert.IsTrue(CreateService().CanPayByCard);
    }

    [TestMethod]
    public void CanPayByCard_IsTrueWhenAFreeItemIsPartOfACartWithAnAmount()
    {
        _cart.AddItem(Item(0m, id: 1), 1);
        _cart.AddItem(Item(5m, id: 2), 1);

        Assert.IsTrue(CreateService().CanPayByCard);
    }

    [TestMethod]
    public void CanPayByCard_FollowsTheCart()
    {
        var service = CreateService();
        _cart.AddItem(Item(5m), 1);
        Assert.IsTrue(service.CanPayByCard);

        _cart.Clear();

        Assert.IsFalse(service.CanPayByCard);
    }

    [TestMethod]
    public void CreateSession_ReturnsANewSessionEveryTime()
    {
        var service = CreateService();

        Assert.AreNotSame(service.CreateSession(), service.CreateSession());
    }

    [TestMethod]
    public void CreateSession_StartsReadyAndShowsTheAmountOfTheCart()
    {
        _cart.AddItem(Item(7.5m), 2);

        var session = CreateService().CreateSession();

        Assert.AreEqual(CardPaymentPhase.NotStarted, session.Phase);
        Assert.AreEqual(15m, session.Amount);
    }

    [TestMethod]
    public async Task CreateSession_UsesTheConfiguredDelay()
    {
        var waits = new List<TimeSpan>();
        var client = Substitute.For<IServerPaymentClient>();
        client.StartCardPaymentAsync(Arg.Any<OrderRequest>(), Arg.Any<CancellationToken>()).Returns(
            Demolite.Core.Models.Result<PaymentStatusResponse>.Success(new PaymentStatusResponse(Guid.NewGuid(), PaymentRequestStatus.Pending)));
        client.GetCardPaymentAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(
            Demolite.Core.Models.Result<PaymentStatusResponse>.Success(new PaymentStatusResponse(Guid.NewGuid(), PaymentRequestStatus.Failed)));
        _cart.AddItem(Item(5m), 1);
        var service = new ClientCardPaymentService(client, _cart, Substitute.For<ICompletedOrderRegistry>(), new ClientCardPaymentOptions(), TimeProvider.System)
        {
            Delay = (time, _) =>
            {
                waits.Add(time);
                return Task.CompletedTask;
            }
        };

        await service.CreateSession().StartAsync();

        Assert.HasCount(1, waits);
        Assert.AreEqual(TimeSpan.FromSeconds(1.5), waits[0]);
    }

    [TestMethod]
    public void Options_HaveTheAgreedDefaults()
    {
        var options = new ClientCardPaymentOptions();

        Assert.AreEqual(TimeSpan.FromSeconds(1.5), options.PollInterval);
        Assert.AreEqual(TimeSpan.FromSeconds(180), options.GiveUpAfter);
    }
}
