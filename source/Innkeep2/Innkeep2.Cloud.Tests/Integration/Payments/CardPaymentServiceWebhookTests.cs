using Innkeep2.Cloud.Services.Payments;
using Innkeep2.Models.Internal.Receipt;
using Innkeep2.Models.Shared;
using Innkeep2.Models.SumUp;
using NSubstitute;

namespace Innkeep2.Cloud.Tests.Integration.Payments;

[TestClass]
public class CardPaymentServiceWebhookTests
{
    private static SumUpWebhookEvent BuildWebhook(string clientTransactionId = CardPaymentServiceFixture.ClientTransactionId, string eventType = SumUpWebhookEvent.SoloTransactionUpdated, string status = "successful") => new()
    {
        Id = "evt_1",
        EventType = eventType,
        Payload = new SumUpWebhookPayload { ClientTransactionId = clientTransactionId, MerchantCode = CardPaymentServiceFixture.MerchantCode, Status = status }
    };

    [DataRow("wrong")]
    [DataRow("")]
    [DataRow(null)]
    [DataRow("secret-token ")]
    [DataRow("SECRET-TOKEN")]
    [TestMethod]
    public async Task Webhook_RejectsAWrongTokenWithoutTouchingThePayment(string? token)
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        var requestId = await fixture.StartAsync();
        fixture.TransactionIs("SUCCESSFUL");

        var result = await fixture.Service.HandleWebhookAsync(token, BuildWebhook());

        Assert.AreEqual(SumUpWebhookResult.Unauthorized, result);
        Assert.AreEqual(PaymentRequestStatus.Pending, (await fixture.LoadAsync(requestId)).Status);
        await fixture.SumUp.DidNotReceiveWithAnyArgs().GetTransactionAsync(default!, default!, default);
    }

    [TestMethod]
    public async Task Webhook_RejectsEverythingWithoutAnActiveAccount()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync(withAccount: false);

        var result = await fixture.Service.HandleWebhookAsync(CardPaymentServiceFixture.WebhookToken, BuildWebhook());

        Assert.AreEqual(SumUpWebhookResult.Unauthorized, result);
    }

    [TestMethod]
    public async Task Webhook_IgnoresOtherEventTypes()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        await fixture.StartAsync();

        var result = await fixture.Service.HandleWebhookAsync(CardPaymentServiceFixture.WebhookToken, BuildWebhook(eventType: "solo.something.else"));

        Assert.AreEqual(SumUpWebhookResult.Ignored, result);
        await fixture.SumUp.DidNotReceiveWithAnyArgs().GetTransactionAsync(default!, default!, default);
    }

    [TestMethod]
    public async Task Webhook_IgnoresAnUnknownClientTransactionId()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        await fixture.StartAsync();

        var result = await fixture.Service.HandleWebhookAsync(CardPaymentServiceFixture.WebhookToken, BuildWebhook("unknown"));

        Assert.AreEqual(SumUpWebhookResult.Ignored, result);
        await fixture.SumUp.DidNotReceiveWithAnyArgs().GetTransactionAsync(default!, default!, default);
    }

    [TestMethod]
    public async Task Webhook_VerifiesAtSumUpImmediatelyAndCompletesThePayment()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        var requestId = await fixture.StartAsync();
        fixture.TransactionIs("SUCCESSFUL");

        var result = await fixture.Service.HandleWebhookAsync(CardPaymentServiceFixture.WebhookToken, BuildWebhook());

        Assert.AreEqual(SumUpWebhookResult.Processed, result);
        var stored = await fixture.LoadAsync(requestId);
        Assert.AreEqual(PaymentRequestStatus.Completed, stored.Status);
        Assert.AreEqual("TEENSK4W2K", stored.SumupTransactionCode);
        await fixture.SumUp.Received(1).GetTransactionAsync(CardPaymentServiceFixture.MerchantCode, CardPaymentServiceFixture.ClientTransactionId, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Webhook_IgnoresTheThrottleOfTheStatusRequests()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        var requestId = await fixture.StartAsync();
        fixture.Advance(fixture.Options.FirstVerificationDelay);
        await fixture.Service.GetStatusAsync(requestId);
        fixture.TransactionIs("SUCCESSFUL");

        await fixture.Service.HandleWebhookAsync(CardPaymentServiceFixture.WebhookToken, BuildWebhook());

        Assert.AreEqual(PaymentRequestStatus.Completed, (await fixture.LoadAsync(requestId)).Status);
    }

    [TestMethod]
    public async Task Webhook_NeverTrustsASuccessThatSumUpDoesNotConfirm()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        var requestId = await fixture.StartAsync();
        fixture.TransactionIs("PENDING");

        await fixture.Service.HandleWebhookAsync(CardPaymentServiceFixture.WebhookToken, BuildWebhook(status: "successful"));

        Assert.AreEqual(PaymentRequestStatus.Pending, (await fixture.LoadAsync(requestId)).Status);
        await fixture.Orders.DidNotReceiveWithAnyArgs().CreateOrderAsync(default!, default, default, default);
    }

    [TestMethod]
    public async Task Webhook_NeverTrustsAFailureThatSumUpDoesNotConfirm()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        var requestId = await fixture.StartAsync();
        fixture.TransactionIs("SUCCESSFUL");

        await fixture.Service.HandleWebhookAsync(CardPaymentServiceFixture.WebhookToken, BuildWebhook(status: "failed"));

        Assert.AreEqual(PaymentRequestStatus.Completed, (await fixture.LoadAsync(requestId)).Status);
    }

    [TestMethod]
    public async Task Webhook_EndsAFailedPaymentWhenSumUpConfirmsTheFailure()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        var requestId = await fixture.StartAsync();
        fixture.TransactionIs("FAILED");

        await fixture.Service.HandleWebhookAsync(CardPaymentServiceFixture.WebhookToken, BuildWebhook(status: "failed"));

        Assert.AreEqual(PaymentRequestStatus.Failed, (await fixture.LoadAsync(requestId)).Status);
    }

    [TestMethod]
    public async Task Webhook_ReportsACancelledPaymentAfterTheUserCancelled()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        var requestId = await fixture.StartAsync();
        var stored = await fixture.LoadAsync(requestId);
        stored.TerminationReason = PaymentTerminationReason.UserCancelled;
        await fixture.Payments.UpdateAsync(stored);
        fixture.TransactionIs("FAILED");

        await fixture.Service.HandleWebhookAsync(CardPaymentServiceFixture.WebhookToken, BuildWebhook(status: "failed"));

        Assert.AreEqual(PaymentRequestStatus.Cancelled, (await fixture.LoadAsync(requestId)).Status);
    }

    [TestMethod]
    public async Task Webhook_DoesNothingForACompletedPayment()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        var requestId = await fixture.StartAsync();
        fixture.TransactionIs("SUCCESSFUL");
        await fixture.Service.HandleWebhookAsync(CardPaymentServiceFixture.WebhookToken, BuildWebhook());

        var again = await fixture.Service.HandleWebhookAsync(CardPaymentServiceFixture.WebhookToken, BuildWebhook());

        Assert.AreEqual(SumUpWebhookResult.Processed, again);
        Assert.AreEqual(PaymentRequestStatus.Completed, (await fixture.LoadAsync(requestId)).Status);
        await fixture.Orders.ReceivedWithAnyArgs(1).CreateOrderAsync(default!, default, default, default);
        await fixture.SumUp.ReceivedWithAnyArgs(1).GetTransactionAsync(default!, default!, default);
    }

    [TestMethod]
    public async Task Webhook_AcceptsTheTokenOfTheActiveAccountOnly()
    {
        var account = CardPaymentServiceFixture.BuildAccount();
        using var fixture = await CardPaymentServiceFixture.CreateAsync(account: account);
        await fixture.StartAsync();

        var result = await fixture.Service.HandleWebhookAsync(account.WebhookToken, BuildWebhook());

        Assert.AreEqual(SumUpWebhookResult.Processed, result);
    }

    [TestMethod]
    public async Task WebhookAndStatusRequestAtTheSameTime_CreateTheOrderOnlyOnce()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        var requestId = await fixture.StartAsync();
        fixture.TransactionIs("SUCCESSFUL");
        fixture.Advance(fixture.Options.FirstVerificationDelay);
        fixture.Orders.CreateOrderAsync(Arg.Any<Innkeep2.Models.Internal.OrderRequest>(), Arg.Any<CancellationToken>(), Arg.Any<bool>(), Arg.Any<string?>())
            .Returns(async _ =>
            {
                await Task.Delay(100);
                return Demolite.Core.Models.Result<TransactionReceipt>.Success(CardPaymentServiceFixture.BuildReceipt());
            });

        await Task.WhenAll(
            fixture.Service.GetStatusAsync(requestId),
            fixture.Service.HandleWebhookAsync(CardPaymentServiceFixture.WebhookToken, BuildWebhook()),
            fixture.Service.GetStatusAsync(requestId));

        await fixture.Orders.ReceivedWithAnyArgs(1).CreateOrderAsync(default!, default, default, default);
        Assert.AreEqual(PaymentRequestStatus.Completed, (await fixture.LoadAsync(requestId)).Status);
    }
}
