using Demolite.Core.Models;
using Innkeep2.Models.Shared;
using NSubstitute;

namespace Innkeep2.Cloud.Tests.Integration.Payments;

[TestClass]
public class CardPaymentServiceCancelTests
{
    [TestMethod]
    public async Task Cancel_ReturnsNotFoundForAnUnknownPayment()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();

        var result = await fixture.Service.CancelAsync(Guid.NewGuid());

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("Payment.NotFound", result.Error!.Code);
    }

    [TestMethod]
    public async Task Cancel_TerminatesTheStoredReaderAndRemembersTheReason()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        var requestId = await fixture.StartAsync();

        var result = await fixture.Service.CancelAsync(requestId);

        Assert.IsTrue(result.IsSuccess);
        await fixture.SumUp.Received(1).TerminateAsync(CardPaymentServiceFixture.MerchantCode, CardPaymentServiceFixture.ReaderId, Arg.Any<CancellationToken>());
        Assert.AreEqual(PaymentTerminationReason.UserCancelled, (await fixture.LoadAsync(requestId)).TerminationReason);
    }

    [TestMethod]
    public async Task Cancel_StaysPendingUntilSumUpConfirmsTheFailure()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        var requestId = await fixture.StartAsync();
        fixture.TransactionIs("PENDING");

        var result = await fixture.Service.CancelAsync(requestId);

        Assert.AreEqual(PaymentRequestStatus.Pending, result.Value!.Status);
        Assert.AreEqual("Warte auf die Bestätigung von SumUp", result.Value.Message);
    }

    [TestMethod]
    public async Task Cancel_ChecksTheOutcomeAtSumUpRightAway()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        var requestId = await fixture.StartAsync();
        fixture.TransactionIs("FAILED");

        var result = await fixture.Service.CancelAsync(requestId);

        Assert.AreEqual(PaymentRequestStatus.Cancelled, result.Value!.Status);
        Assert.AreEqual("Die Zahlung wurde abgebrochen", result.Value.Message);
        Assert.AreEqual(PaymentRequestStatus.Cancelled, (await fixture.LoadAsync(requestId)).Status);
    }

    [TestMethod]
    public async Task Cancel_EndsAsCancelledWhenSumUpConfirmsLater()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        var requestId = await fixture.StartAsync();
        await fixture.Service.CancelAsync(requestId);
        fixture.TransactionIs("FAILED");
        fixture.Advance(fixture.Options.FirstVerificationDelay);

        var result = await fixture.Service.GetStatusAsync(requestId);

        Assert.AreEqual(PaymentRequestStatus.Cancelled, result.Value!.Status);
    }

    [TestMethod]
    public async Task Cancel_CompletesThePaymentWhenTheCustomerPaidJustBeforeTheCancel()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        var requestId = await fixture.StartAsync();
        fixture.TransactionIs("SUCCESSFUL");

        var result = await fixture.Service.CancelAsync(requestId);

        Assert.AreEqual(PaymentRequestStatus.Completed, result.Value!.Status);
        Assert.IsNotNull(result.Value.Receipt);
        await fixture.Orders.ReceivedWithAnyArgs(1).CreateOrderAsync(default!, default, default, default);
    }

    [TestMethod]
    public async Task Cancel_KeepsThePaymentUntouchedWhenTheReaderCannotBeTerminated()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        var requestId = await fixture.StartAsync();
        fixture.SumUp.TerminateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Http.400", "not awaiting cardholder")));

        var result = await fixture.Service.CancelAsync(requestId);

        Assert.AreEqual(PaymentRequestStatus.Pending, result.Value!.Status);
        Assert.AreEqual("Der Abbruch ist am Kartenleser nicht mehr möglich", result.Value.Message);
        Assert.IsNull((await fixture.LoadAsync(requestId)).TerminationReason);
        await fixture.SumUp.DidNotReceiveWithAnyArgs().GetTransactionAsync(default!, default!, default);
    }

    [TestMethod]
    public async Task Cancel_AllowsANewAttemptAfterAFailedTerminate()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        var requestId = await fixture.StartAsync();
        fixture.SumUp.TerminateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Http.400", "no")), Result.Success());

        await fixture.Service.CancelAsync(requestId);
        await fixture.Service.CancelAsync(requestId);

        await fixture.SumUp.ReceivedWithAnyArgs(2).TerminateAsync(default!, default!, default);
        Assert.AreEqual(PaymentTerminationReason.UserCancelled, (await fixture.LoadAsync(requestId)).TerminationReason);
    }

    [DataRow(PaymentRequestStatus.Paid)]
    [DataRow(PaymentRequestStatus.Completed)]
    [DataRow(PaymentRequestStatus.Failed)]
    [DataRow(PaymentRequestStatus.Cancelled)]
    [DataRow(PaymentRequestStatus.Expired)]
    [TestMethod]
    public async Task Cancel_DoesNotTerminateAPaymentThatIsNotPending(PaymentRequestStatus status)
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        var requestId = await fixture.StartAsync();
        var stored = await fixture.LoadAsync(requestId);
        stored.Status = status;
        await fixture.Payments.UpdateAsync(stored);

        var result = await fixture.Service.CancelAsync(requestId);

        Assert.IsTrue(result.IsSuccess);
        await fixture.SumUp.DidNotReceiveWithAnyArgs().TerminateAsync(default!, default!, default);
        Assert.IsNull((await fixture.LoadAsync(requestId)).TerminationReason);
    }

    [TestMethod]
    public async Task Cancel_DoesNotTerminateAgainOnceAReasonIsRecorded()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        var requestId = await fixture.StartAsync();
        await fixture.Service.CancelAsync(requestId);

        var result = await fixture.Service.CancelAsync(requestId);

        Assert.AreEqual(PaymentRequestStatus.Pending, result.Value!.Status);
        await fixture.SumUp.ReceivedWithAnyArgs(1).TerminateAsync(default!, default!, default);
    }

    [TestMethod]
    public async Task Cancel_UsesTheStoredReaderAndNotTheCurrentlyActiveOne()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        var requestId = await fixture.StartAsync();
        var stored = await fixture.LoadAsync(requestId);
        stored.ReaderId = "rdr_stored";
        stored.MerchantCode = "STORED01";
        await fixture.Payments.UpdateAsync(stored);

        await fixture.Service.CancelAsync(requestId);

        await fixture.SumUp.Received(1).TerminateAsync("STORED01", "rdr_stored", Arg.Any<CancellationToken>());
    }
}
