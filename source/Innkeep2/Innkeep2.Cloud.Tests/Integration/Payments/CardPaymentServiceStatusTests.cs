using Demolite.Core.Models;
using Innkeep2.Models.Internal;
using Innkeep2.Models.Internal.Receipt;
using Innkeep2.Models.Shared;
using NSubstitute;

namespace Innkeep2.Cloud.Tests.Integration.Payments;

[TestClass]
public class CardPaymentServiceStatusTests
{
    private static async Task<(CardPaymentServiceFixture Fixture, Guid RequestId)> StartedAsync()
    {
        var fixture = await CardPaymentServiceFixture.CreateAsync();
        var requestId = await fixture.StartAsync();

        return (fixture, requestId);
    }

    private static void AdvancePastFirstVerification(CardPaymentServiceFixture fixture)
        => fixture.Advance(fixture.Options.FirstVerificationDelay);

    [TestMethod]
    public async Task GetStatus_ReturnsNotFoundForAnUnknownPayment()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();

        var result = await fixture.Service.GetStatusAsync(Guid.NewGuid());

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("Payment.NotFound", result.Error!.Code);
    }

    [TestMethod]
    public async Task GetStatus_DoesNotAskSumUpBeforeTheFirstVerificationDelay()
    {
        var (fixture, requestId) = await StartedAsync();
        using var scope = fixture;
        fixture.Advance(fixture.Options.FirstVerificationDelay - TimeSpan.FromMilliseconds(1));

        var result = await fixture.Service.GetStatusAsync(requestId);

        Assert.AreEqual(PaymentRequestStatus.Pending, result.Value!.Status);
        await fixture.SumUp.DidNotReceiveWithAnyArgs().GetTransactionAsync(default!, default!, default);
    }

    [TestMethod]
    public async Task GetStatus_AsksSumUpWithTheStoredMerchantAndClientTransactionId()
    {
        var (fixture, requestId) = await StartedAsync();
        using var scope = fixture;
        AdvancePastFirstVerification(fixture);

        await fixture.Service.GetStatusAsync(requestId);

        await fixture.SumUp.Received(1).GetTransactionAsync(CardPaymentServiceFixture.MerchantCode, CardPaymentServiceFixture.ClientTransactionId, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task GetStatus_CreatesTheOrderOnceSumUpConfirmsTheSuccess()
    {
        var (fixture, requestId) = await StartedAsync();
        using var scope = fixture;
        fixture.TransactionIs("SUCCESSFUL");
        AdvancePastFirstVerification(fixture);

        var result = await fixture.Service.GetStatusAsync(requestId);

        Assert.AreEqual(PaymentRequestStatus.Completed, result.Value!.Status);
        Assert.IsNotNull(result.Value.Receipt);
        Assert.IsNull(result.Value.Message);
        await fixture.Orders.Received(1).CreateOrderAsync(
            Arg.Is<OrderRequest>(x => x.RequestId == requestId && x.PaymentType == PaymentType.NonCash && x.AmountGiven == 15m),
            Arg.Any<CancellationToken>(),
            true,
            "TEENSK4W2K");
        var stored = await fixture.LoadAsync(requestId);
        Assert.AreEqual(PaymentRequestStatus.Completed, stored.Status);
        Assert.AreEqual("TEENSK4W2K", stored.SumupTransactionCode);
    }

    [TestMethod]
    public async Task GetStatus_ReturnsTheReceiptOfTheCreatedOrder()
    {
        var (fixture, requestId) = await StartedAsync();
        using var scope = fixture;
        var receipt = CardPaymentServiceFixture.BuildReceipt();
        fixture.Orders.GetReceiptAsync(requestId, Arg.Any<CancellationToken>()).Returns(receipt);
        fixture.TransactionIs("SUCCESSFUL");
        AdvancePastFirstVerification(fixture);

        var result = await fixture.Service.GetStatusAsync(requestId);

        Assert.AreSame(receipt, result.Value!.Receipt);
    }

    [DataRow(null, PaymentRequestStatus.Failed)]
    [DataRow(PaymentTerminationReason.UserCancelled, PaymentRequestStatus.Cancelled)]
    [DataRow(PaymentTerminationReason.Timeout, PaymentRequestStatus.Expired)]
    [TestMethod]
    public async Task GetStatus_EndsTheFailedPaymentOnlyWhenSumUpConfirmsTheFailure(PaymentTerminationReason? reason, PaymentRequestStatus expected)
    {
        var (fixture, requestId) = await StartedAsync();
        using var scope = fixture;
        var stored = await fixture.LoadAsync(requestId);
        stored.TerminationReason = reason;
        await fixture.Payments.UpdateAsync(stored);
        fixture.TransactionIs("FAILED");
        AdvancePastFirstVerification(fixture);

        var result = await fixture.Service.GetStatusAsync(requestId);

        Assert.AreEqual(expected, result.Value!.Status);
        Assert.AreEqual(expected, (await fixture.LoadAsync(requestId)).Status);
        await fixture.Orders.DidNotReceiveWithAnyArgs().CreateOrderAsync(default!, default, default, default);
    }

    [TestMethod]
    public async Task GetStatus_TreatsACancelledSumUpTransactionAsFailed()
    {
        var (fixture, requestId) = await StartedAsync();
        using var scope = fixture;
        fixture.TransactionIs("CANCELLED");
        AdvancePastFirstVerification(fixture);

        var result = await fixture.Service.GetStatusAsync(requestId);

        Assert.AreEqual(PaymentRequestStatus.Failed, result.Value!.Status);
        Assert.AreEqual("Die Zahlung ist fehlgeschlagen", result.Value.Message);
    }

    [DataRow("PENDING")]
    [DataRow("REFUNDED")]
    [DataRow("SOMETHING_NEW")]
    [TestMethod]
    public async Task GetStatus_KeepsThePaymentPendingWhenSumUpHasNoFinalOutcome(string sumUpStatus)
    {
        var (fixture, requestId) = await StartedAsync();
        using var scope = fixture;
        fixture.TransactionIs(sumUpStatus);
        AdvancePastFirstVerification(fixture);

        var result = await fixture.Service.GetStatusAsync(requestId);

        Assert.AreEqual(PaymentRequestStatus.Pending, result.Value!.Status);
        await fixture.Orders.DidNotReceiveWithAnyArgs().CreateOrderAsync(default!, default, default, default);
    }

    [TestMethod]
    public async Task GetStatus_KeepsThePaymentPendingWhenTheLookupFails()
    {
        var (fixture, requestId) = await StartedAsync();
        using var scope = fixture;
        fixture.LookupFails();
        AdvancePastFirstVerification(fixture);

        var result = await fixture.Service.GetStatusAsync(requestId);

        Assert.AreEqual(PaymentRequestStatus.Pending, result.Value!.Status);
    }

    [DataRow(14.99)]
    [DataRow(150.0)]
    [DataRow(0.0)]
    [TestMethod]
    public async Task GetStatus_DoesNotCompleteWhenSumUpReportsAnotherAmount(double amount)
    {
        var (fixture, requestId) = await StartedAsync();
        using var scope = fixture;
        fixture.TransactionIs("SUCCESSFUL", amount: (decimal)amount);
        AdvancePastFirstVerification(fixture);

        var result = await fixture.Service.GetStatusAsync(requestId);

        Assert.AreEqual(PaymentRequestStatus.Pending, result.Value!.Status);
        await fixture.Orders.DidNotReceiveWithAnyArgs().CreateOrderAsync(default!, default, default, default);
    }

    [TestMethod]
    public async Task GetStatus_DoesNotCompleteWhenSumUpReportsAnotherCurrency()
    {
        var (fixture, requestId) = await StartedAsync();
        using var scope = fixture;
        fixture.TransactionIs("SUCCESSFUL", currency: "USD");
        AdvancePastFirstVerification(fixture);

        var result = await fixture.Service.GetStatusAsync(requestId);

        Assert.AreEqual(PaymentRequestStatus.Pending, result.Value!.Status);
    }

    [TestMethod]
    public async Task GetStatus_AcceptsTheCurrencyInAnyCase()
    {
        var (fixture, requestId) = await StartedAsync();
        using var scope = fixture;
        fixture.TransactionIs("SUCCESSFUL", currency: "eur");
        AdvancePastFirstVerification(fixture);

        var result = await fixture.Service.GetStatusAsync(requestId);

        Assert.AreEqual(PaymentRequestStatus.Completed, result.Value!.Status);
    }

    [TestMethod]
    public async Task GetStatus_AsksSumUpAtMostOncePerInterval()
    {
        var (fixture, requestId) = await StartedAsync();
        using var scope = fixture;
        AdvancePastFirstVerification(fixture);

        await fixture.Service.GetStatusAsync(requestId);
        await fixture.Service.GetStatusAsync(requestId);
        fixture.Advance(fixture.Options.VerificationInterval - TimeSpan.FromMilliseconds(1));
        await fixture.Service.GetStatusAsync(requestId);

        await fixture.SumUp.ReceivedWithAnyArgs(1).GetTransactionAsync(default!, default!, default);

        fixture.Advance(TimeSpan.FromMilliseconds(1));
        await fixture.Service.GetStatusAsync(requestId);

        await fixture.SumUp.ReceivedWithAnyArgs(2).GetTransactionAsync(default!, default!, default);
    }

    [TestMethod]
    public async Task GetStatus_KeepsThePaidPaymentAndRetriesWhenTheOrderCannotBeCreated()
    {
        var (fixture, requestId) = await StartedAsync();
        using var scope = fixture;
        fixture.Orders.CreateOrderAsync(Arg.Any<OrderRequest>(), Arg.Any<CancellationToken>(), Arg.Any<bool>(), Arg.Any<string?>()).Returns(
            Result<TransactionReceipt>.Failure(new Error("Order.NoConfiguration", "no event")),
            Result<TransactionReceipt>.Success(CardPaymentServiceFixture.BuildReceipt()));
        fixture.TransactionIs("SUCCESSFUL");
        AdvancePastFirstVerification(fixture);

        var first = await fixture.Service.GetStatusAsync(requestId);
        var second = await fixture.Service.GetStatusAsync(requestId);

        Assert.AreEqual(PaymentRequestStatus.Paid, first.Value!.Status);
        Assert.AreEqual("Zahlung erhalten, die Bestellung wird angelegt", first.Value.Message);
        Assert.AreEqual(PaymentRequestStatus.Completed, second.Value!.Status);
        await fixture.Orders.Received(2).CreateOrderAsync(Arg.Is<OrderRequest>(x => x.RequestId == requestId), Arg.Any<CancellationToken>(), true, "TEENSK4W2K");
        await fixture.SumUp.ReceivedWithAnyArgs(1).GetTransactionAsync(default!, default!, default);
    }

    [TestMethod]
    public async Task GetStatus_NeverAsksSumUpOrCreatesAnOrderAgainForACompletedPayment()
    {
        var (fixture, requestId) = await StartedAsync();
        using var scope = fixture;
        fixture.TransactionIs("SUCCESSFUL");
        AdvancePastFirstVerification(fixture);
        await fixture.Service.GetStatusAsync(requestId);
        fixture.Advance(TimeSpan.FromMinutes(10));

        var again = await fixture.Service.GetStatusAsync(requestId);

        Assert.AreEqual(PaymentRequestStatus.Completed, again.Value!.Status);
        Assert.IsNotNull(again.Value.Receipt);
        await fixture.SumUp.ReceivedWithAnyArgs(1).GetTransactionAsync(default!, default!, default);
        await fixture.Orders.ReceivedWithAnyArgs(1).CreateOrderAsync(default!, default, default, default);
    }

    [DataRow(PaymentRequestStatus.Failed)]
    [DataRow(PaymentRequestStatus.Cancelled)]
    [DataRow(PaymentRequestStatus.Expired)]
    [TestMethod]
    public async Task GetStatus_DoesNotAskSumUpAgainForAnEndedPayment(PaymentRequestStatus status)
    {
        var (fixture, requestId) = await StartedAsync();
        using var scope = fixture;
        var stored = await fixture.LoadAsync(requestId);
        stored.Status = status;
        await fixture.Payments.UpdateAsync(stored);
        fixture.TransactionIs("SUCCESSFUL");
        fixture.Advance(TimeSpan.FromMinutes(10));

        var result = await fixture.Service.GetStatusAsync(requestId);

        Assert.AreEqual(status, result.Value!.Status);
        await fixture.SumUp.DidNotReceiveWithAnyArgs().GetTransactionAsync(default!, default!, default);
        await fixture.Orders.DidNotReceiveWithAnyArgs().CreateOrderAsync(default!, default, default, default);
    }

    [DataRow(PaymentRequestStatus.Failed, "Die Zahlung ist fehlgeschlagen")]
    [DataRow(PaymentRequestStatus.Cancelled, "Die Zahlung wurde abgebrochen")]
    [DataRow(PaymentRequestStatus.Expired, "Zeitüberschreitung, die Zahlung wurde nicht abgeschlossen")]
    [TestMethod]
    public async Task GetStatus_ShowsAClearMessageForEveryEndedPayment(PaymentRequestStatus status, string expectedMessage)
    {
        var (fixture, requestId) = await StartedAsync();
        using var scope = fixture;
        var stored = await fixture.LoadAsync(requestId);
        stored.Status = status;
        await fixture.Payments.UpdateAsync(stored);

        var result = await fixture.Service.GetStatusAsync(requestId);

        Assert.AreEqual(expectedMessage, result.Value!.Message);
    }

    [TestMethod]
    public async Task GetStatus_TerminatesTheReaderOnceAfterTheOverallTimeoutButStaysPending()
    {
        var (fixture, requestId) = await StartedAsync();
        using var scope = fixture;
        fixture.Advance(fixture.Options.OverallTimeout);

        var result = await fixture.Service.GetStatusAsync(requestId);

        Assert.AreEqual(PaymentRequestStatus.Pending, result.Value!.Status);
        Assert.AreEqual("Warte auf die Bestätigung von SumUp", result.Value.Message);
        await fixture.SumUp.Received(1).TerminateAsync(CardPaymentServiceFixture.MerchantCode, CardPaymentServiceFixture.ReaderId, Arg.Any<CancellationToken>());
        Assert.AreEqual(PaymentTerminationReason.Timeout, (await fixture.LoadAsync(requestId)).TerminationReason);
    }

    [TestMethod]
    public async Task GetStatus_DoesNotTerminateTwiceAndExpiresOnlyWhenSumUpConfirmsTheFailure()
    {
        var (fixture, requestId) = await StartedAsync();
        using var scope = fixture;
        fixture.Advance(fixture.Options.OverallTimeout);
        await fixture.Service.GetStatusAsync(requestId);
        fixture.Advance(fixture.Options.VerificationInterval);

        var stillPending = await fixture.Service.GetStatusAsync(requestId);
        fixture.TransactionIs("FAILED");
        fixture.Advance(fixture.Options.VerificationInterval);
        var expired = await fixture.Service.GetStatusAsync(requestId);

        Assert.AreEqual(PaymentRequestStatus.Pending, stillPending.Value!.Status);
        Assert.AreEqual(PaymentRequestStatus.Expired, expired.Value!.Status);
        await fixture.SumUp.ReceivedWithAnyArgs(1).TerminateAsync(default!, default!, default);
    }

    [TestMethod]
    public async Task GetStatus_RemembersTheTimeoutEvenWhenTheTerminateFails()
    {
        var (fixture, requestId) = await StartedAsync();
        using var scope = fixture;
        fixture.SumUp.TerminateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Http.400", "not awaiting cardholder")));
        fixture.Advance(fixture.Options.OverallTimeout);

        await fixture.Service.GetStatusAsync(requestId);
        fixture.Advance(fixture.Options.VerificationInterval);
        await fixture.Service.GetStatusAsync(requestId);

        await fixture.SumUp.ReceivedWithAnyArgs(1).TerminateAsync(default!, default!, default);
        Assert.AreEqual(PaymentTerminationReason.Timeout, (await fixture.LoadAsync(requestId)).TerminationReason);
    }

    [TestMethod]
    public async Task GetStatus_StillCompletesWhenTheCustomerPaidAfterTheTimeout()
    {
        var (fixture, requestId) = await StartedAsync();
        using var scope = fixture;
        fixture.Advance(fixture.Options.OverallTimeout);
        fixture.TransactionIs("SUCCESSFUL");

        var result = await fixture.Service.GetStatusAsync(requestId);

        Assert.AreEqual(PaymentRequestStatus.Completed, result.Value!.Status);
        await fixture.Orders.ReceivedWithAnyArgs(1).CreateOrderAsync(default!, default, default, default);
    }

    [TestMethod]
    public async Task GetStatus_UsesTheTimeoutOptions()
    {
        var options = new Innkeep2.Cloud.Services.Payments.CardPaymentOptions { OverallTimeout = TimeSpan.FromSeconds(20) };
        using var fixture = await CardPaymentServiceFixture.CreateAsync(options: options);
        var requestId = await fixture.StartAsync();
        fixture.Advance(TimeSpan.FromSeconds(19));
        await fixture.Service.GetStatusAsync(requestId);
        await fixture.SumUp.DidNotReceiveWithAnyArgs().TerminateAsync(default!, default!, default);

        fixture.Advance(TimeSpan.FromSeconds(1));
        await fixture.Service.GetStatusAsync(requestId);

        await fixture.SumUp.ReceivedWithAnyArgs(1).TerminateAsync(default!, default!, default);
    }

    [TestMethod]
    public async Task GetStatus_SkipsTheLookupForAPaymentWithoutClientTransactionId()
    {
        var (fixture, requestId) = await StartedAsync();
        using var scope = fixture;
        var stored = await fixture.LoadAsync(requestId);
        stored.ClientTransactionId = null;
        await fixture.Payments.UpdateAsync(stored);
        AdvancePastFirstVerification(fixture);

        var result = await fixture.Service.GetStatusAsync(requestId);

        Assert.AreEqual(PaymentRequestStatus.Pending, result.Value!.Status);
        await fixture.SumUp.DidNotReceiveWithAnyArgs().GetTransactionAsync(default!, default!, default);
    }

    [TestMethod]
    public async Task GetStatus_UsesTheStoredReaderAndNotTheCurrentlyActiveOne()
    {
        var (fixture, requestId) = await StartedAsync();
        using var scope = fixture;
        var stored = await fixture.LoadAsync(requestId);
        stored.ReaderId = "rdr_stored";
        stored.MerchantCode = "STORED01";
        await fixture.Payments.UpdateAsync(stored);
        fixture.Advance(fixture.Options.OverallTimeout);

        await fixture.Service.GetStatusAsync(requestId);

        await fixture.SumUp.Received(1).TerminateAsync("STORED01", "rdr_stored", Arg.Any<CancellationToken>());
        await fixture.SumUp.Received(1).GetTransactionAsync("STORED01", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
