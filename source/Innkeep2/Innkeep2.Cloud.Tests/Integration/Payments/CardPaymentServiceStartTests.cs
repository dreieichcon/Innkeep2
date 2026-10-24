using Demolite.Core.Models;
using Innkeep2.CredentialsDb.Models;
using Innkeep2.Models.Shared;
using Innkeep2.Models.SumUp;
using NSubstitute;

namespace Innkeep2.Cloud.Tests.Integration.Payments;

[TestClass]
public class CardPaymentServiceStartTests
{
    [TestMethod]
    public async Task Start_StoresAPendingPaymentWithTheReaderAndTheOrder()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        var order = CardPaymentServiceFixture.BuildOrder(price: 15.5m);

        var result = await fixture.Service.StartAsync(order);

        Assert.IsTrue(result.IsSuccess);
        var stored = await fixture.LoadAsync(order.RequestId);
        Assert.AreEqual(PaymentRequestStatus.Pending, stored.Status);
        Assert.AreEqual(15.5m, stored.TotalAmount);
        Assert.AreEqual("EUR", stored.Currency);
        Assert.AreEqual(CardPaymentServiceFixture.MerchantCode, stored.MerchantCode);
        Assert.AreEqual(CardPaymentServiceFixture.ReaderId, stored.ReaderId);
        Assert.AreEqual(CardPaymentServiceFixture.ClientTransactionId, stored.ClientTransactionId);
        Assert.AreEqual("chk-1", stored.CheckoutId);
        Assert.AreEqual(fixture.Time.GetUtcNow().UtcDateTime, stored.CreatedAt);
        StringAssert.Contains(stored.OrderRequestJson, order.RequestId.ToString());
    }

    [TestMethod]
    public async Task Start_ReturnsPendingWithAMessageAndNoReceipt()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        var order = CardPaymentServiceFixture.BuildOrder();

        var result = await fixture.Service.StartAsync(order);

        Assert.AreEqual(order.RequestId, result.Value!.RequestId);
        Assert.AreEqual(PaymentRequestStatus.Pending, result.Value.Status);
        Assert.AreEqual("Bitte am Kartenleser bezahlen", result.Value.Message);
        Assert.IsNull(result.Value.Receipt);
    }

    [TestMethod]
    public async Task Start_SendsTheCheckoutToTheReaderWithAmountInMinorUnits()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();

        await fixture.Service.StartAsync(CardPaymentServiceFixture.BuildOrder(price: 15.5m));

        await fixture.SumUp.Received(1).CreateCheckoutAsync(
            CardPaymentServiceFixture.MerchantCode,
            CardPaymentServiceFixture.ReaderId,
            Arg.Is<SumUpCheckoutRequest>(x => x.TotalAmount.Value == 1550 && x.TotalAmount.MinorUnit == 2 && x.TotalAmount.Currency == "EUR"),
            Arg.Any<CancellationToken>());
    }

    [DataRow("https://cloud.example")]
    [DataRow("https://cloud.example/")]
    [TestMethod]
    public async Task Start_UsesThePublicUrlAndTheWebhookTokenAsReturnUrl(string baseUrl)
    {
        var account = CardPaymentServiceFixture.BuildAccount();
        account.PublicBaseUrl = baseUrl;
        using var fixture = await CardPaymentServiceFixture.CreateAsync(account: account);

        await fixture.Service.StartAsync(CardPaymentServiceFixture.BuildOrder());

        await fixture.SumUp.Received(1).CreateCheckoutAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Is<SumUpCheckoutRequest>(x => x.ReturnUrl == "https://cloud.example/webhooks/sumup/secret-token"),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Start_SendsTheAffiliateWhenAppIdAndKeyAreSet()
    {
        var account = CardPaymentServiceFixture.BuildAccount();
        account.AffiliateAppId = "app.example";
        account.AffiliateKey = "affiliate-key";
        using var fixture = await CardPaymentServiceFixture.CreateAsync(account: account);
        var order = CardPaymentServiceFixture.BuildOrder();

        await fixture.Service.StartAsync(order);

        await fixture.SumUp.Received(1).CreateCheckoutAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Is<SumUpCheckoutRequest>(x => x.Affiliate != null
                                              && x.Affiliate.AppId == "app.example"
                                              && x.Affiliate.Key == "affiliate-key"
                                              && x.Affiliate.ForeignTransactionId == order.RequestId.ToString()),
            Arg.Any<CancellationToken>());
    }

    [DataRow(null, null)]
    [DataRow("app.example", null)]
    [DataRow(null, "affiliate-key")]
    [DataRow("", "affiliate-key")]
    [DataRow("app.example", " ")]
    [TestMethod]
    public async Task Start_OmitsTheAffiliateUnlessBothValuesAreSet(string? appId, string? key)
    {
        var account = CardPaymentServiceFixture.BuildAccount();
        account.AffiliateAppId = appId;
        account.AffiliateKey = key;
        using var fixture = await CardPaymentServiceFixture.CreateAsync(account: account);

        await fixture.Service.StartAsync(CardPaymentServiceFixture.BuildOrder());

        await fixture.SumUp.Received(1).CreateCheckoutAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Is<SumUpCheckoutRequest>(x => x.Affiliate == null),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Start_DoesNotCreateAnOrder()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();

        await fixture.Service.StartAsync(CardPaymentServiceFixture.BuildOrder());

        await fixture.Orders.DidNotReceiveWithAnyArgs().CreateOrderAsync(default!, default, default, default);
    }

    [TestMethod]
    public async Task Start_RejectsCashPayments()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();

        var result = await fixture.Service.StartAsync(CardPaymentServiceFixture.BuildOrder(paymentType: PaymentType.Cash));

        Assert.AreEqual("Payment.NotCard", result.Error!.Code);
        await fixture.SumUp.DidNotReceiveWithAnyArgs().CreateCheckoutAsync(default!, default!, default!, default);
    }

    [TestMethod]
    public async Task Start_RejectsOrdersWithoutItems()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        var order = CardPaymentServiceFixture.BuildOrder() with { Items = [] };

        var result = await fixture.Service.StartAsync(order);

        Assert.AreEqual("Payment.NoItems", result.Error!.Code);
        await fixture.SumUp.DidNotReceiveWithAnyArgs().CreateCheckoutAsync(default!, default!, default!, default);
    }

    [TestMethod]
    public async Task Start_RejectsAnAmountOfZero()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();

        var result = await fixture.Service.StartAsync(CardPaymentServiceFixture.BuildOrder(price: 0m));

        Assert.AreEqual("Payment.NoAmount", result.Error!.Code);
        await fixture.SumUp.DidNotReceiveWithAnyArgs().CreateCheckoutAsync(default!, default!, default!, default);
    }

    [DataRow(10)]
    [DataRow(20)]
    [DataRow(0)]
    [TestMethod]
    public async Task Start_RejectsAGivenAmountThatDiffersFromTheTotal(int given)
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();

        var result = await fixture.Service.StartAsync(CardPaymentServiceFixture.BuildOrder(price: 15m, amountGiven: given));

        Assert.AreEqual("Payment.AmountMismatch", result.Error!.Code);
        await fixture.SumUp.DidNotReceiveWithAnyArgs().CreateCheckoutAsync(default!, default!, default!, default);
    }

    [TestMethod]
    public async Task Start_WithoutActiveAccount_FailsWithoutStoringAnything()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync(withAccount: false);
        var order = CardPaymentServiceFixture.BuildOrder();

        var result = await fixture.Service.StartAsync(order);

        Assert.AreEqual("Payment.NoAccount", result.Error!.Code);
        Assert.IsFalse((await fixture.Payments.GetByRequestIdAsync(order.RequestId)).IsSuccess);
    }

    [TestMethod]
    public async Task Start_WithoutActiveReader_FailsWithoutStoringAnything()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync(withReader: false);
        var order = CardPaymentServiceFixture.BuildOrder();

        var result = await fixture.Service.StartAsync(order);

        Assert.AreEqual("Payment.NoReader", result.Error!.Code);
        Assert.IsFalse((await fixture.Payments.GetByRequestIdAsync(order.RequestId)).IsSuccess);
        await fixture.SumUp.DidNotReceiveWithAnyArgs().CreateCheckoutAsync(default!, default!, default!, default);
    }

    [TestMethod]
    public async Task Start_WithAReaderOfAnotherMerchant_Fails()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync(reader: CardPaymentServiceFixture.BuildReader("OTHER123"));

        var result = await fixture.Service.StartAsync(CardPaymentServiceFixture.BuildOrder());

        Assert.AreEqual("Payment.MerchantMismatch", result.Error!.Code);
        await fixture.SumUp.DidNotReceiveWithAnyArgs().CreateCheckoutAsync(default!, default!, default!, default);
    }

    [TestMethod]
    public async Task Start_RejectsAnotherPaymentWhileTheReaderIsBusy()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        await fixture.StartAsync();
        var second = CardPaymentServiceFixture.BuildOrder();

        var result = await fixture.Service.StartAsync(second);

        Assert.AreEqual("Payment.ReaderBusy", result.Error!.Code);
        Assert.IsFalse((await fixture.Payments.GetByRequestIdAsync(second.RequestId)).IsSuccess);
        await fixture.SumUp.ReceivedWithAnyArgs(1).CreateCheckoutAsync(default!, default!, default!, default);
    }

    [TestMethod]
    public async Task Start_AcceptsAPaymentOnceThePendingOneIsOlderThanTheBusyWindow()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        await fixture.StartAsync();
        fixture.Advance(fixture.Options.ReaderBusyWindow + TimeSpan.FromSeconds(1));

        var result = await fixture.Service.StartAsync(CardPaymentServiceFixture.BuildOrder());

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(PaymentRequestStatus.Pending, result.Value!.Status);
    }

    [DataRow(PaymentRequestStatus.Failed)]
    [DataRow(PaymentRequestStatus.Cancelled)]
    [DataRow(PaymentRequestStatus.Expired)]
    [DataRow(PaymentRequestStatus.Completed)]
    [DataRow(PaymentRequestStatus.Paid)]
    [TestMethod]
    public async Task Start_AcceptsAPaymentWhenThePreviousOneIsNotPending(PaymentRequestStatus status)
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        var firstId = await fixture.StartAsync();
        var first = await fixture.LoadAsync(firstId);
        first.Status = status;
        await fixture.Payments.UpdateAsync(first);

        var result = await fixture.Service.StartAsync(CardPaymentServiceFixture.BuildOrder());

        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public async Task Start_WithAnExistingRequestId_ReturnsTheStatusWithoutANewCheckout()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        var order = CardPaymentServiceFixture.BuildOrder();
        await fixture.Service.StartAsync(order);

        var again = await fixture.Service.StartAsync(order);

        Assert.IsTrue(again.IsSuccess);
        Assert.AreEqual(PaymentRequestStatus.Pending, again.Value!.Status);
        await fixture.SumUp.ReceivedWithAnyArgs(1).CreateCheckoutAsync(default!, default!, default!, default);
    }

    [TestMethod]
    public async Task Start_WithAnExistingFailedRequestId_ReturnsFailedAndDoesNotRetry()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        fixture.SumUp.CreateCheckoutAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SumUpCheckoutRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<SumUpCheckoutResponse>.Failure(new Error("Http.422", "invalid")));
        var order = CardPaymentServiceFixture.BuildOrder();
        await fixture.Service.StartAsync(order);

        var again = await fixture.Service.StartAsync(order);

        Assert.AreEqual(PaymentRequestStatus.Failed, again.Value!.Status);
        await fixture.SumUp.ReceivedWithAnyArgs(1).CreateCheckoutAsync(default!, default!, default!, default);
    }

    [DataRow("Http.401")]
    [DataRow("Http.404")]
    [DataRow("Http.422")]
    [DataRow("Http.500")]
    [DataRow("Http.Timeout")]
    [TestMethod]
    public async Task Start_MarksThePaymentFailedWhenSumUpRejectsTheCheckout(string errorCode)
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        fixture.SumUp.CreateCheckoutAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SumUpCheckoutRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<SumUpCheckoutResponse>.Failure(new Error(errorCode, "secret SumUp detail")));
        var order = CardPaymentServiceFixture.BuildOrder();

        var result = await fixture.Service.StartAsync(order);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(PaymentRequestStatus.Failed, result.Value!.Status);
        Assert.AreEqual("Die Zahlung ist fehlgeschlagen", result.Value.Message);
        Assert.DoesNotContain("secret SumUp detail", result.Value.Message!);
        var stored = await fixture.LoadAsync(order.RequestId);
        Assert.AreEqual(PaymentRequestStatus.Failed, stored.Status);
        StringAssert.Contains(stored.FailureReason, errorCode);
        Assert.IsNull(stored.ClientTransactionId);
    }

    [TestMethod]
    public async Task Start_AFailedStartDoesNotBlockTheReader()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        fixture.SumUp.CreateCheckoutAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SumUpCheckoutRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<SumUpCheckoutResponse>.Failure(new Error("Http.500", "down")));
        await fixture.Service.StartAsync(CardPaymentServiceFixture.BuildOrder());
        fixture.SumUp.CreateCheckoutAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SumUpCheckoutRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<SumUpCheckoutResponse>.Success(new SumUpCheckoutResponse { Data = new SumUpCheckoutData { ClientTransactionId = "ctx-2" } }));

        var result = await fixture.Service.StartAsync(CardPaymentServiceFixture.BuildOrder());

        Assert.AreEqual(PaymentRequestStatus.Pending, result.Value!.Status);
    }
}
