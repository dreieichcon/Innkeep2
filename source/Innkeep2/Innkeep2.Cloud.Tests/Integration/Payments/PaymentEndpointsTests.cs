using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Demolite.Core.Models;
using Innkeep2.Cloud.Api;
using Innkeep2.Models.Internal;
using Innkeep2.Models.Internal.Receipt;
using Innkeep2.Models.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Innkeep2.Cloud.Tests.Integration.Payments;

[TestClass]
public class PaymentEndpointsTests
{
    private const string WebhookBody = """{"id":"evt_1","event_type":"solo.transaction.updated","payload":{"client_transaction_id":"ctx-1","merchant_code":"MH4H92C7","status":"successful"},"timestamp":"2026-10-24T10:00:00Z"}""";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static async Task<(WebApplication App, HttpClient Client)> StartHostAsync(CardPaymentServiceFixture fixture)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Services.AddSingleton(fixture.Service);

        var app = builder.Build();
        app.MapCardPaymentEndpoints();
        app.MapSumUpWebhook();
        await app.StartAsync();

        return (app, new HttpClient(new SocketsHttpHandler { UseProxy = false }) { BaseAddress = new Uri(app.Urls.First()) });
    }

    private static StringContent AsJson(string json)
        => new(json, Encoding.UTF8, "application/json");

    private static async Task<PaymentStatusResponse> ReadStatusAsync(HttpResponseMessage response)
        => (await response.Content.ReadFromJsonAsync<PaymentStatusResponse>(Json))!;

    private static async Task WaitUntilAsync(Func<Task<bool>> condition)
    {
        var timer = Stopwatch.StartNew();

        while (timer.Elapsed < TimeSpan.FromSeconds(5))
        {
            if (await condition())
                return;

            await Task.Delay(25);
        }

        Assert.Fail("The condition was not met in time.");
    }

    [TestMethod]
    public async Task Start_ReturnsThePendingStatusAsJson()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        var (app, client) = await StartHostAsync(fixture);
        await using var _ = app;
        using var __ = client;
        var order = CardPaymentServiceFixture.BuildOrder();

        var response = await client.PostAsJsonAsync("/payments/card/start", order, Json);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var status = await ReadStatusAsync(response);
        Assert.AreEqual(order.RequestId, status.RequestId);
        Assert.AreEqual(PaymentRequestStatus.Pending, status.Status);
        Assert.AreEqual("Bitte am Kartenleser bezahlen", status.Message);
    }

    [TestMethod]
    public async Task Start_SendsTheEnumsAsReadableStrings()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        var (app, client) = await StartHostAsync(fixture);
        await using var _ = app;
        using var __ = client;

        var response = await client.PostAsJsonAsync("/payments/card/start", CardPaymentServiceFixture.BuildOrder(), Json);

        StringAssert.Contains(await response.Content.ReadAsStringAsync(), "\"Pending\"");
    }

    [TestMethod]
    public async Task Start_ReturnsBadRequestWithTheErrorForARejectedOrder()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        var (app, client) = await StartHostAsync(fixture);
        await using var _ = app;
        using var __ = client;

        var response = await client.PostAsJsonAsync("/payments/card/start", CardPaymentServiceFixture.BuildOrder(paymentType: PaymentType.Cash), Json);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        StringAssert.Contains(body, "Payment.NotCard");
    }

    [TestMethod]
    public async Task Start_ReturnsBadRequestWhenTheReaderIsBusy()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        await fixture.StartAsync();
        var (app, client) = await StartHostAsync(fixture);
        await using var _ = app;
        using var __ = client;

        var response = await client.PostAsJsonAsync("/payments/card/start", CardPaymentServiceFixture.BuildOrder(), Json);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        StringAssert.Contains(await response.Content.ReadAsStringAsync(), "Payment.ReaderBusy");
    }

    [TestMethod]
    public async Task Start_ReturnsBadRequestForAnInvalidBody()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        var (app, client) = await StartHostAsync(fixture);
        await using var _ = app;
        using var __ = client;

        var response = await client.PostAsync("/payments/card/start", AsJson("not json"));

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Status_ReturnsTheStatusOfAStartedPayment()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        var requestId = await fixture.StartAsync();
        var (app, client) = await StartHostAsync(fixture);
        await using var _ = app;
        using var __ = client;

        var response = await client.GetAsync($"/payments/card/{requestId}");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual(PaymentRequestStatus.Pending, (await ReadStatusAsync(response)).Status);
    }

    [TestMethod]
    public async Task Status_ReturnsTheReceiptOfACompletedPayment()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        var requestId = await fixture.StartAsync();
        fixture.TransactionIs("SUCCESSFUL");
        fixture.Advance(fixture.Options.FirstVerificationDelay);
        var (app, client) = await StartHostAsync(fixture);
        await using var _ = app;
        using var __ = client;

        var response = await client.GetAsync($"/payments/card/{requestId}");

        var status = await ReadStatusAsync(response);
        Assert.AreEqual(PaymentRequestStatus.Completed, status.Status);
        Assert.IsNotNull(status.Receipt);
        Assert.AreEqual(PaymentType.NonCash, status.Receipt.PaymentType);
    }

    [TestMethod]
    public async Task Status_ReturnsNotFoundWithABodyForAnUnknownPayment()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        var (app, client) = await StartHostAsync(fixture);
        await using var _ = app;
        using var __ = client;

        var response = await client.GetAsync($"/payments/card/{Guid.NewGuid()}");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        StringAssert.Contains(await response.Content.ReadAsStringAsync(), "Payment.NotFound");
    }

    [TestMethod]
    public async Task Status_RejectsARequestIdThatIsNotAGuid()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        var (app, client) = await StartHostAsync(fixture);
        await using var _ = app;
        using var __ = client;

        var response = await client.GetAsync("/payments/card/not-a-guid");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task Cancel_TerminatesTheReaderAndReturnsTheStatus()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        var requestId = await fixture.StartAsync();
        var (app, client) = await StartHostAsync(fixture);
        await using var _ = app;
        using var __ = client;

        var response = await client.PostAsync($"/payments/card/{requestId}/cancel", null);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("Warte auf die Bestätigung von SumUp", (await ReadStatusAsync(response)).Message);
        await fixture.SumUp.ReceivedWithAnyArgs(1).TerminateAsync(default!, default!, default);
    }

    [TestMethod]
    public async Task Cancel_ReturnsNotFoundForAnUnknownPayment()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        var (app, client) = await StartHostAsync(fixture);
        await using var _ = app;
        using var __ = client;

        var response = await client.PostAsync($"/payments/card/{Guid.NewGuid()}/cancel", null);

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task Webhook_WithAWrongTokenReturnsNotFoundWithABodyAndChangesNothing()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        var requestId = await fixture.StartAsync();
        fixture.TransactionIs("SUCCESSFUL");
        var (app, client) = await StartHostAsync(fixture);
        await using var _ = app;
        using var __ = client;

        var response = await client.PostAsync("/webhooks/sumup/wrong-token", AsJson(WebhookBody));

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        Assert.AreNotEqual(string.Empty, await response.Content.ReadAsStringAsync());
        await Task.Delay(100);
        Assert.AreEqual(PaymentRequestStatus.Pending, (await fixture.LoadAsync(requestId)).Status);
    }

    [TestMethod]
    public async Task Webhook_WithTheRightTokenAnswersWithAnEmptyOkAndCompletesThePayment()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        var requestId = await fixture.StartAsync();
        fixture.TransactionIs("SUCCESSFUL");
        var (app, client) = await StartHostAsync(fixture);
        await using var _ = app;
        using var __ = client;

        var response = await client.PostAsync($"/webhooks/sumup/{CardPaymentServiceFixture.WebhookToken}", AsJson(WebhookBody));

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual(string.Empty, await response.Content.ReadAsStringAsync());
        await WaitUntilAsync(async () => (await fixture.LoadAsync(requestId)).Status == PaymentRequestStatus.Completed);
    }

    [TestMethod]
    public async Task Webhook_AnswersBeforeTheOrderIsCreated()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        var requestId = await fixture.StartAsync();
        fixture.TransactionIs("SUCCESSFUL");
        fixture.Orders.CreateOrderAsync(Arg.Any<OrderRequest>(), Arg.Any<CancellationToken>(), Arg.Any<bool>(), Arg.Any<string?>())
            .Returns(async _ =>
            {
                await Task.Delay(1500);
                return Result<TransactionReceipt>.Success(CardPaymentServiceFixture.BuildReceipt());
            });
        var (app, client) = await StartHostAsync(fixture);
        await using var _ = app;
        using var __ = client;
        var timer = Stopwatch.StartNew();

        var response = await client.PostAsync($"/webhooks/sumup/{CardPaymentServiceFixture.WebhookToken}", AsJson(WebhookBody));

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsLessThan(1000, timer.ElapsedMilliseconds);
        await WaitUntilAsync(async () => (await fixture.LoadAsync(requestId)).Status == PaymentRequestStatus.Completed);
    }

    [TestMethod]
    public async Task Webhook_StillAnswersOkWhenTheProcessingFails()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        await fixture.StartAsync();
        fixture.SumUp.GetTransactionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Result<Innkeep2.Models.SumUp.SumUpTransaction>>(_ => throw new InvalidOperationException("boom"));
        var (app, client) = await StartHostAsync(fixture);
        await using var _ = app;
        using var __ = client;

        var response = await client.PostAsync($"/webhooks/sumup/{CardPaymentServiceFixture.WebhookToken}", AsJson(WebhookBody));
        await Task.Delay(100);
        var again = await client.GetAsync("/payments/card/" + Guid.NewGuid());

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, again.StatusCode);
    }

    [TestMethod]
    public async Task Webhook_AnswersOkForEventsItIgnores()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        var (app, client) = await StartHostAsync(fixture);
        await using var _ = app;
        using var __ = client;

        var response = await client.PostAsync(
            $"/webhooks/sumup/{CardPaymentServiceFixture.WebhookToken}",
            AsJson(WebhookBody.Replace("solo.transaction.updated", "solo.other")));

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task Webhook_ReturnsBadRequestForAnInvalidBody()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        var (app, client) = await StartHostAsync(fixture);
        await using var _ = app;
        using var __ = client;

        var response = await client.PostAsync($"/webhooks/sumup/{CardPaymentServiceFixture.WebhookToken}", AsJson("not json"));

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Webhook_OnlyAcceptsPost()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync();
        var (app, client) = await StartHostAsync(fixture);
        await using var _ = app;
        using var __ = client;

        var response = await client.GetAsync($"/webhooks/sumup/{CardPaymentServiceFixture.WebhookToken}");

        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [TestMethod]
    public void IsWebhookTokenValid_AcceptsOnlyTheTokenOfTheActiveAccount()
    {
        using var fixture = CardPaymentServiceFixture.CreateAsync().GetAwaiter().GetResult();

        Assert.IsTrue(fixture.Service.IsWebhookTokenValid(CardPaymentServiceFixture.WebhookToken));
        Assert.IsFalse(fixture.Service.IsWebhookTokenValid("other"));
        Assert.IsFalse(fixture.Service.IsWebhookTokenValid(null));
        Assert.IsFalse(fixture.Service.IsWebhookTokenValid(string.Empty));
    }

    [TestMethod]
    public async Task IsWebhookTokenValid_IsFalseWithoutAnActiveAccount()
    {
        using var fixture = await CardPaymentServiceFixture.CreateAsync(withAccount: false);

        Assert.IsFalse(fixture.Service.IsWebhookTokenValid(CardPaymentServiceFixture.WebhookToken));
    }
}
