using Demolite.Core.Models;
using Innkeep2.Models.Internal;
using Innkeep2.Models.Internal.Receipt;
using Innkeep2.Models.Shared;
using NSubstitute;

namespace Innkeep2.Cloud.Tests.Integration.Payments;

[TestClass]
public class CardPaymentServiceOverviewActionsTests
{
    private static async Task<(CardPaymentServiceFixture Fixture, Guid RequestId)> StartedAsync(PaymentRequestStatus? status = null, string? code = "TEENSK4W2K")
    {
        var fixture = await CardPaymentServiceFixture.CreateAsync();
        var requestId = await fixture.StartAsync();
        var stored = await fixture.LoadAsync(requestId);

        if (status is { } wanted)
            stored.Status = wanted;

        stored.SumupTransactionCode = code;
        await fixture.Payments.UpdateAsync(stored);

        return (fixture, requestId);
    }

    #region Recheck

    [TestMethod]
    public async Task Recheck_ReturnsNotFoundForAnUnknownPayment()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();

        var result = await fixture.Service.RecheckAsync(Guid.NewGuid());

        Assert.AreEqual("Payment.NotFound", result.Error!.Code);
    }

    [TestMethod]
    public async Task Recheck_AsksSumUpRightAwayEvenBeforeTheUsualDelayAndInterval()
    {
        var (fixture, requestId) = await StartedAsync();
        using var scope = fixture;

        await fixture.Service.RecheckAsync(requestId);
        await fixture.Service.RecheckAsync(requestId);

        await fixture.SumUp.ReceivedWithAnyArgs(2).GetTransactionAsync(default!, default!, default);
    }

    [TestMethod]
    public async Task Recheck_CompletesAPaymentThatSumUpShowsAsSuccessful()
    {
        var (fixture, requestId) = await StartedAsync();
        using var scope = fixture;
        fixture.TransactionIs("SUCCESSFUL");

        var result = await fixture.Service.RecheckAsync(requestId);

        Assert.AreEqual(PaymentRequestStatus.Completed, result.Value!.Status);
        Assert.IsNotNull(result.Value.Receipt);
        await fixture.Orders.Received(1).CreateOrderAsync(Arg.Is<OrderRequest>(x => x.RequestId == requestId), Arg.Any<CancellationToken>(), true, "TEENSK4W2K");
    }

    [DataRow(null, PaymentRequestStatus.Failed)]
    [DataRow(PaymentTerminationReason.Timeout, PaymentRequestStatus.Expired)]
    [DataRow(PaymentTerminationReason.UserCancelled, PaymentRequestStatus.Cancelled)]
    [TestMethod]
    public async Task Recheck_EndsAPaymentThatSumUpShowsAsFailedWithTheRightStatus(PaymentTerminationReason? reason, PaymentRequestStatus expected)
    {
        var (fixture, requestId) = await StartedAsync();
        using var scope = fixture;
        var stored = await fixture.LoadAsync(requestId);
        stored.TerminationReason = reason;
        await fixture.Payments.UpdateAsync(stored);
        fixture.TransactionIs("FAILED");

        var result = await fixture.Service.RecheckAsync(requestId);

        Assert.AreEqual(expected, result.Value!.Status);
    }

    [TestMethod]
    public async Task Recheck_KeepsAPaymentPendingThatSumUpStillShowsAsPending()
    {
        var (fixture, requestId) = await StartedAsync();
        using var scope = fixture;
        fixture.TransactionIs("PENDING");

        var result = await fixture.Service.RecheckAsync(requestId);

        Assert.AreEqual(PaymentRequestStatus.Pending, result.Value!.Status);
    }

    [TestMethod]
    public async Task Recheck_TerminatesAnOverdueReaderOnceLikeTheNormalStatusRequest()
    {
        var (fixture, requestId) = await StartedAsync();
        using var scope = fixture;
        fixture.Advance(TimeSpan.FromHours(2));

        await fixture.Service.RecheckAsync(requestId);
        await fixture.Service.RecheckAsync(requestId);

        await fixture.SumUp.ReceivedWithAnyArgs(1).TerminateAsync(default!, default!, default);
    }

    [DataRow(PaymentRequestStatus.Failed)]
    [DataRow(PaymentRequestStatus.Cancelled)]
    [DataRow(PaymentRequestStatus.Expired)]
    [TestMethod]
    public async Task Recheck_NeverChangesAnEndedPayment(PaymentRequestStatus status)
    {
        var (fixture, requestId) = await StartedAsync(status);
        using var scope = fixture;
        fixture.TransactionIs("SUCCESSFUL");

        var result = await fixture.Service.RecheckAsync(requestId);

        Assert.AreEqual(status, result.Value!.Status);
        await fixture.SumUp.DidNotReceiveWithAnyArgs().GetTransactionAsync(default!, default!, default);
    }

    #endregion

    #region Retry order

    [TestMethod]
    public async Task RetryOrder_ReturnsNotFoundForAnUnknownPayment()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();

        var result = await fixture.Service.RetryOrderAsync(Guid.NewGuid());

        Assert.AreEqual("Payment.NotFound", result.Error!.Code);
    }

    [TestMethod]
    public async Task RetryOrder_CreatesTheOrderOfAPaidPaymentAndCompletesIt()
    {
        var (fixture, requestId) = await StartedAsync(PaymentRequestStatus.Paid);
        using var scope = fixture;

        var result = await fixture.Service.RetryOrderAsync(requestId);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(PaymentRequestStatus.Completed, result.Value!.Status);
        Assert.IsNotNull(result.Value.Receipt);
        await fixture.Orders.Received(1).CreateOrderAsync(Arg.Is<OrderRequest>(x => x.RequestId == requestId && x.PaymentType == PaymentType.NonCash), Arg.Any<CancellationToken>(), true, "TEENSK4W2K");
        Assert.AreEqual(PaymentRequestStatus.Completed, (await fixture.LoadAsync(requestId)).Status);
    }

    [TestMethod]
    public async Task RetryOrder_KeepsAPaidPaymentPaidAndReportsAFailedOrder()
    {
        var (fixture, requestId) = await StartedAsync(PaymentRequestStatus.Paid);
        using var scope = fixture;
        fixture.Orders.CreateOrderAsync(Arg.Any<OrderRequest>(), Arg.Any<CancellationToken>(), Arg.Any<bool>(), Arg.Any<string?>())
            .Returns(Result<TransactionReceipt>.Failure(new Error("Order.NoConfiguration", "no event")));

        var result = await fixture.Service.RetryOrderAsync(requestId);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("Payment.OrderFailed", result.Error!.Code);
        Assert.AreEqual(PaymentRequestStatus.Paid, (await fixture.LoadAsync(requestId)).Status);
    }

    [TestMethod]
    public async Task RetryOrder_CanBeRepeatedAfterAFailedAttempt()
    {
        var (fixture, requestId) = await StartedAsync(PaymentRequestStatus.Paid);
        using var scope = fixture;
        fixture.Orders.CreateOrderAsync(Arg.Any<OrderRequest>(), Arg.Any<CancellationToken>(), Arg.Any<bool>(), Arg.Any<string?>())
            .Returns(Result<TransactionReceipt>.Failure(new Error("Order.NoConfiguration", "no event")), Result<TransactionReceipt>.Success(CardPaymentServiceFixture.BuildReceipt()));

        var first = await fixture.Service.RetryOrderAsync(requestId);
        var second = await fixture.Service.RetryOrderAsync(requestId);

        Assert.IsFalse(first.IsSuccess);
        Assert.AreEqual(PaymentRequestStatus.Completed, second.Value!.Status);
    }

    [TestMethod]
    public async Task RetryOrder_CreatesTheMissingOrderOfACompletedPayment()
    {
        var (fixture, requestId) = await StartedAsync(PaymentRequestStatus.Completed);
        using var scope = fixture;
        fixture.Orders.GetReceiptAsync(requestId, Arg.Any<CancellationToken>()).Returns((TransactionReceipt?)null);

        var result = await fixture.Service.RetryOrderAsync(requestId);

        Assert.IsTrue(result.IsSuccess);
        await fixture.Orders.Received(1).CreateOrderAsync(Arg.Is<OrderRequest>(x => x.RequestId == requestId), Arg.Any<CancellationToken>(), true, "TEENSK4W2K");
        Assert.AreEqual(PaymentRequestStatus.Completed, (await fixture.LoadAsync(requestId)).Status);
    }

    [TestMethod]
    public async Task RetryOrder_ReportsAFailedOrderForACompletedPaymentWithoutOrder()
    {
        var (fixture, requestId) = await StartedAsync(PaymentRequestStatus.Completed);
        using var scope = fixture;
        fixture.Orders.GetReceiptAsync(requestId, Arg.Any<CancellationToken>()).Returns((TransactionReceipt?)null);
        fixture.Orders.CreateOrderAsync(Arg.Any<OrderRequest>(), Arg.Any<CancellationToken>(), Arg.Any<bool>(), Arg.Any<string?>())
            .Returns(Result<TransactionReceipt>.Failure(new Error("Order.NoConfiguration", "no event")));

        var result = await fixture.Service.RetryOrderAsync(requestId);

        Assert.AreEqual("Payment.OrderFailed", result.Error!.Code);
    }

    [TestMethod]
    public async Task RetryOrder_DoesNothingWhenACompletedPaymentAlreadyHasItsOrder()
    {
        var (fixture, requestId) = await StartedAsync(PaymentRequestStatus.Completed);
        using var scope = fixture;

        var result = await fixture.Service.RetryOrderAsync(requestId);

        Assert.AreEqual("Payment.NothingToRetry", result.Error!.Code);
        await fixture.Orders.DidNotReceiveWithAnyArgs().CreateOrderAsync(default!, default, default, default);
    }

    [DataRow(PaymentRequestStatus.Pending)]
    [DataRow(PaymentRequestStatus.Failed)]
    [DataRow(PaymentRequestStatus.Cancelled)]
    [DataRow(PaymentRequestStatus.Expired)]
    [TestMethod]
    public async Task RetryOrder_NeverCreatesAnOrderForAPaymentSumUpDidNotConfirm(PaymentRequestStatus status)
    {
        var (fixture, requestId) = await StartedAsync(status);
        using var scope = fixture;

        var result = await fixture.Service.RetryOrderAsync(requestId);

        Assert.AreEqual("Payment.NothingToRetry", result.Error!.Code);
        await fixture.Orders.DidNotReceiveWithAnyArgs().CreateOrderAsync(default!, default, default, default);
    }

    [TestMethod]
    public async Task RetryOrder_ReportsAnUnreadableStoredOrder()
    {
        var (fixture, requestId) = await StartedAsync(PaymentRequestStatus.Paid);
        using var scope = fixture;
        var stored = await fixture.LoadAsync(requestId);
        stored.OrderRequestJson = "not json";
        await fixture.Payments.UpdateAsync(stored);

        var result = await fixture.Service.RetryOrderAsync(requestId);

        Assert.AreEqual("Payment.OrderFailed", result.Error!.Code);
        await fixture.Orders.DidNotReceiveWithAnyArgs().CreateOrderAsync(default!, default, default, default);
    }

    #endregion
}
