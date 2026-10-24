using Demolite.Core.Models;
using Innkeep2.Cloud.Services.Payments;
using NSubstitute;

namespace Innkeep2.Cloud.Tests.Integration.Payments;

[TestClass]
public class CardRefundServiceTests
{
    [TestMethod]
    public async Task Refund_LooksTheTransactionUpAndRefundsItCompletelyAtSumUp()
    {
        using var fixture = new CardRefundServiceFixture();
        var requestId = await fixture.AddPaymentAsync();

        var result = await fixture.Service.RefundAsync(requestId);

        Assert.IsTrue(result.IsSuccess);
        await fixture.SumUp.Received(1).GetTransactionAsync(CardRefundServiceFixture.MerchantCode, CardRefundServiceFixture.ClientTransactionId, Arg.Any<CancellationToken>());
        await fixture.SumUp.Received(1).RefundTransactionAsync(CardRefundServiceFixture.MerchantCode, CardRefundServiceFixture.SumUpTransactionId, null, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Refund_UsesTheMerchantAndClientTransactionIdOfTheStoredPayment()
    {
        using var fixture = new CardRefundServiceFixture();
        var requestId = await fixture.AddPaymentAsync(clientTransactionId: "other-ctx", merchantCode: "OTHER123");

        await fixture.Service.RefundAsync(requestId);

        await fixture.SumUp.Received(1).GetTransactionAsync("OTHER123", "other-ctx", Arg.Any<CancellationToken>());
        await fixture.SumUp.Received(1).RefundTransactionAsync("OTHER123", Arg.Any<string>(), Arg.Any<decimal?>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Refund_RefundsOnlyTheRestOfAPartiallyRefundedPayment()
    {
        using var fixture = new CardRefundServiceFixture();
        var requestId = await fixture.AddPaymentAsync();
        fixture.TransactionIs(amount: 15m, refunded: 5m);

        var result = await fixture.Service.RefundAsync(requestId);

        Assert.IsTrue(result.IsSuccess);
        await fixture.SumUp.Received(1).RefundTransactionAsync(Arg.Any<string>(), Arg.Any<string>(), 10m, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Refund_TreatsAZeroRefundedAmountAsNotRefundedYet()
    {
        using var fixture = new CardRefundServiceFixture();
        var requestId = await fixture.AddPaymentAsync();
        fixture.TransactionIs(refunded: 0m);

        await fixture.Service.RefundAsync(requestId);

        await fixture.SumUp.Received(1).RefundTransactionAsync(Arg.Any<string>(), Arg.Any<string>(), null, Arg.Any<CancellationToken>());
    }

    [DataRow("REFUNDED")]
    [DataRow("refunded")]
    [TestMethod]
    public async Task Refund_DoesNotRefundAgainWhenSumUpShowsTheTransactionAsRefunded(string status)
    {
        using var fixture = new CardRefundServiceFixture();
        var requestId = await fixture.AddPaymentAsync();
        fixture.TransactionIs(status);

        var result = await fixture.Service.RefundAsync(requestId);

        Assert.IsTrue(result.IsSuccess);
        await fixture.SumUp.DidNotReceiveWithAnyArgs().RefundTransactionAsync(default!, default!, default, default);
    }

    [DataRow(15.0, 15.0)]
    [DataRow(15.0, 20.0)]
    [TestMethod]
    public async Task Refund_DoesNotRefundAgainWhenTheWholeAmountIsAlreadyRefunded(double amount, double refunded)
    {
        using var fixture = new CardRefundServiceFixture();
        var requestId = await fixture.AddPaymentAsync();
        fixture.TransactionIs(amount: (decimal)amount, refunded: (decimal)refunded);

        var result = await fixture.Service.RefundAsync(requestId);

        Assert.IsTrue(result.IsSuccess);
        await fixture.SumUp.DidNotReceiveWithAnyArgs().RefundTransactionAsync(default!, default!, default, default);
    }

    [TestMethod]
    public async Task Refund_FallsBackToTheStoredTotalWhenSumUpReportsNoAmount()
    {
        using var fixture = new CardRefundServiceFixture();
        var requestId = await fixture.AddPaymentAsync(total: 15m);
        fixture.TransactionIs(amount: null, refunded: 15m);

        var result = await fixture.Service.RefundAsync(requestId);

        Assert.IsTrue(result.IsSuccess);
        await fixture.SumUp.DidNotReceiveWithAnyArgs().RefundTransactionAsync(default!, default!, default, default);
    }

    [DataRow("PENDING")]
    [DataRow("FAILED")]
    [DataRow("CANCELLED")]
    [DataRow("SOMETHING_NEW")]
    [DataRow("")]
    [TestMethod]
    public async Task Refund_RefusesPaymentsThatAreNotSuccessfulAtSumUp(string status)
    {
        using var fixture = new CardRefundServiceFixture();
        var requestId = await fixture.AddPaymentAsync();
        fixture.TransactionIs(status);

        var result = await fixture.Service.RefundAsync(requestId);

        Assert.AreEqual(CardRefundErrors.NotRefundable, result.Error!.Code);
        await fixture.SumUp.DidNotReceiveWithAnyArgs().RefundTransactionAsync(default!, default!, default, default);
    }

    [TestMethod]
    public async Task Refund_FailsForAnOrderWithoutAStoredPayment()
    {
        using var fixture = new CardRefundServiceFixture();

        var result = await fixture.Service.RefundAsync(Guid.NewGuid());

        Assert.AreEqual(CardRefundErrors.PaymentMissing, result.Error!.Code);
        await fixture.SumUp.DidNotReceiveWithAnyArgs().GetTransactionAsync(default!, default!, default);
    }

    [TestMethod]
    public async Task Refund_FailsForAPaymentThatNeverGotAClientTransactionId()
    {
        using var fixture = new CardRefundServiceFixture();
        var requestId = await fixture.AddPaymentAsync(clientTransactionId: null);

        var result = await fixture.Service.RefundAsync(requestId);

        Assert.AreEqual(CardRefundErrors.PaymentMissing, result.Error!.Code);
        await fixture.SumUp.DidNotReceiveWithAnyArgs().GetTransactionAsync(default!, default!, default);
    }

    [DataRow("Http.404", CardRefundErrors.PaymentMissing)]
    [DataRow("Http.401", CardRefundErrors.NotPermitted)]
    [DataRow("Http.403", CardRefundErrors.NotPermitted)]
    [DataRow("Http.500", CardRefundErrors.Unavailable)]
    [DataRow("Http.Timeout", CardRefundErrors.Unavailable)]
    [DataRow("Http.RequestFailed", CardRefundErrors.Unavailable)]
    [DataRow("Http.503", CardRefundErrors.Unavailable)]
    [TestMethod]
    public async Task Refund_MapsLookupFailuresWithoutRefunding(string sumUpCode, string expectedCode)
    {
        using var fixture = new CardRefundServiceFixture();
        var requestId = await fixture.AddPaymentAsync();
        fixture.LookupFails(sumUpCode);

        var result = await fixture.Service.RefundAsync(requestId);

        Assert.AreEqual(expectedCode, result.Error!.Code);
        Assert.DoesNotContain("secret SumUp detail", result.Error.Message);
        await fixture.SumUp.DidNotReceiveWithAnyArgs().RefundTransactionAsync(default!, default!, default, default);
    }

    [DataRow(null)]
    [DataRow("")]
    [DataRow("  ")]
    [TestMethod]
    public async Task Refund_FailsWhenSumUpDeliversNoTransactionId(string? id)
    {
        using var fixture = new CardRefundServiceFixture();
        var requestId = await fixture.AddPaymentAsync();
        fixture.TransactionIs(id: id);

        var result = await fixture.Service.RefundAsync(requestId);

        Assert.AreEqual(CardRefundErrors.Unavailable, result.Error!.Code);
        await fixture.SumUp.DidNotReceiveWithAnyArgs().RefundTransactionAsync(default!, default!, default, default);
    }

    [DataRow("Http.401", CardRefundErrors.NotPermitted)]
    [DataRow("Http.403", CardRefundErrors.NotPermitted)]
    [DataRow("Http.404", CardRefundErrors.PaymentMissing)]
    [DataRow("Http.409", CardRefundErrors.NotRefundable)]
    [DataRow("Http.400", CardRefundErrors.Rejected)]
    [DataRow("Http.422", CardRefundErrors.Rejected)]
    [DataRow("Http.500", CardRefundErrors.Unavailable)]
    [DataRow("Http.Timeout", CardRefundErrors.Unavailable)]
    [DataRow("Http.RequestFailed", CardRefundErrors.Unavailable)]
    [TestMethod]
    public async Task Refund_MapsRefundErrorsToClearErrors(string sumUpCode, string expectedCode)
    {
        using var fixture = new CardRefundServiceFixture();
        var requestId = await fixture.AddPaymentAsync();
        fixture.RefundFails(sumUpCode);

        var result = await fixture.Service.RefundAsync(requestId);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(expectedCode, result.Error!.Code);
        Assert.DoesNotContain("secret SumUp detail", result.Error.Message);
    }

    [TestMethod]
    public async Task Refund_TellsThatTheApiKeyNeedsTheRefundPermission()
    {
        using var fixture = new CardRefundServiceFixture();
        var requestId = await fixture.AddPaymentAsync();
        fixture.RefundFails("Http.403");

        var result = await fixture.Service.RefundAsync(requestId);

        StringAssert.Contains(result.Error!.Message, "refunds.write");
    }

    [TestMethod]
    public async Task Refund_CanBeRetriedAfterAFailedAttempt()
    {
        using var fixture = new CardRefundServiceFixture();
        var requestId = await fixture.AddPaymentAsync();
        fixture.SumUp.RefundTransactionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<decimal?>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Http.500", "down")), Result.Success());

        var first = await fixture.Service.RefundAsync(requestId);
        var second = await fixture.Service.RefundAsync(requestId);

        Assert.IsFalse(first.IsSuccess);
        Assert.IsTrue(second.IsSuccess);
        await fixture.SumUp.ReceivedWithAnyArgs(2).RefundTransactionAsync(default!, default!, default, default);
    }

    [TestMethod]
    public async Task Refund_AfterACrashBetweenRefundAndBookingTheRetryDoesNotRefundTwice()
    {
        using var fixture = new CardRefundServiceFixture();
        var requestId = await fixture.AddPaymentAsync();

        await fixture.Service.RefundAsync(requestId);
        fixture.TransactionIs("REFUNDED", refunded: 15m);
        var retry = await fixture.Service.RefundAsync(requestId);

        Assert.IsTrue(retry.IsSuccess);
        await fixture.SumUp.ReceivedWithAnyArgs(1).RefundTransactionAsync(default!, default!, default, default);
    }

    [TestMethod]
    public async Task Refund_TwoRequestsAtTheSameTimeRefundOnlyOnce()
    {
        using var fixture = new CardRefundServiceFixture();
        var requestId = await fixture.AddPaymentAsync();
        fixture.SumUp.RefundTransactionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<decimal?>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                await Task.Delay(100);
                fixture.TransactionIs("REFUNDED", refunded: 15m);
                return Result.Success();
            });

        var results = await Task.WhenAll(fixture.Service.RefundAsync(requestId), fixture.Service.RefundAsync(requestId));

        Assert.IsTrue(results.All(x => x.IsSuccess));
        await fixture.SumUp.ReceivedWithAnyArgs(1).RefundTransactionAsync(default!, default!, default, default);
    }

    [TestMethod]
    public async Task Refund_DifferentOrdersDoNotBlockEachOther()
    {
        using var fixture = new CardRefundServiceFixture();
        var first = await fixture.AddPaymentAsync(clientTransactionId: "ctx-a");
        var second = await fixture.AddPaymentAsync(clientTransactionId: "ctx-b");

        var results = await Task.WhenAll(fixture.Service.RefundAsync(first), fixture.Service.RefundAsync(second));

        Assert.IsTrue(results.All(x => x.IsSuccess));
        await fixture.SumUp.ReceivedWithAnyArgs(2).RefundTransactionAsync(default!, default!, default, default);
    }

    [TestMethod]
    public void ErrorCodes_AreTheOnesTheServerAndTheCloudEndpointRelyOn()
    {
        Assert.AreEqual("Refund.CardPaymentMissing", CardRefundErrors.PaymentMissing);
        Assert.AreEqual("Refund.CardNotRefundable", CardRefundErrors.NotRefundable);
        Assert.AreEqual("Refund.CardRefundRejected", CardRefundErrors.Rejected);
        Assert.AreEqual("Refund.CardNotPermitted", CardRefundErrors.NotPermitted);
        Assert.AreEqual("Refund.CardUnavailable", CardRefundErrors.Unavailable);
    }
}
