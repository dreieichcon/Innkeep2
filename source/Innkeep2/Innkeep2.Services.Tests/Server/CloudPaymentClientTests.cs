using System.Net;
using System.Text.Json;
using Demolite.Core.Models;
using Innkeep2.CredentialsDb;
using Innkeep2.CredentialsDb.Interfaces;
using Innkeep2.CredentialsDb.Models;
using Innkeep2.Models.Internal;
using Innkeep2.Models.Internal.Receipt;
using Innkeep2.Models.Shared;
using Innkeep2.Requests.Cloud;
using Innkeep2.Services.Extensions;
using Innkeep2.Services.Server;
using Innkeep2.Services.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Innkeep2.Services.Tests.Server;

[TestClass]
public class CloudPaymentClientTests
{
    private const string ApiKey = "server-api-key";

    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private sealed class Host : IDisposable
    {
        private readonly ServiceProvider _provider;

        private Host(string? url)
        {
            var repository = Substitute.For<ICredentialRepository<InnkeepApiKey>>();
            repository.GetActiveAsync().Returns(url is null
                ? Result<InnkeepApiKey>.Failure(new Error("NotFound", "none"))
                : Result<InnkeepApiKey>.Success(new InnkeepApiKey { Name = "Cloud", Active = true, ApiKey = ApiKey, Url = url }));

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(repository);
            services.AddSingleton<ActiveCredentialProvider<InnkeepApiKey>>();
            services.AddInnkeepApiClient<CloudPaymentClient>(timeoutSeconds: 15);
            services.AddHttpClient<CloudPaymentClient>().ConfigurePrimaryHttpMessageHandler(() => Handler);
            _provider = services.BuildServiceProvider();
        }

        public StubHttpMessageHandler Handler { get; } = new();

        public CloudPaymentClient Client => _provider.GetRequiredService<CloudPaymentClient>();

        public static async Task<Host> CreateAsync(string? url = "https://cloud.example")
        {
            var host = new Host(url);
            await host._provider.GetRequiredService<ActiveCredentialProvider<InnkeepApiKey>>().ReloadAsync();

            return host;
        }

        public void Dispose() => _provider.Dispose();
    }

    private static OrderRequest BuildOrder() => new()
    {
        RequestId = Guid.NewGuid(),
        Items = [new SalesItem { Id = 7, VariationId = 0, Name = "Item", Price = 10m, TaxRate = 19m, Quantity = 2 }],
        PaymentType = PaymentType.NonCash,
        AmountGiven = 20m,
        Currency = "EUR"
    };

    private static string StatusJson(Guid id, PaymentRequestStatus status, string? message = null, TransactionReceipt? receipt = null)
        => JsonSerializer.Serialize(new PaymentStatusResponse(id, status, message, receipt), Web);

    private static TransactionReceipt BuildReceipt() => new()
    {
        OrderId = Guid.NewGuid(),
        TransactionType = TransactionType.Sale,
        Title = "Title",
        Header = "Header",
        BookingTime = new DateTime(2026, 10, 24, 12, 0, 0, DateTimeKind.Utc),
        Currency = "EUR",
        Lines = [],
        Sum = new ReceiptSum { TotalAmount = 20m, AmountGiven = 20m, AmountReturned = 0m },
        TaxInformation = [],
        Vouchers = [],
        PaymentType = PaymentType.NonCash,
        PretixOrderCode = "ABC12"
    };

    [TestMethod]
    public async Task Start_PostsTheOrderToTheCloudWithTheApiKey()
    {
        using var host = await Host.CreateAsync();
        var order = BuildOrder();
        host.Handler.Enqueue(HttpStatusCode.OK, StatusJson(order.RequestId, PaymentRequestStatus.Pending));

        await host.Client.StartCardPaymentAsync(order);

        var request = host.Handler.Requests.Single();
        Assert.AreEqual(HttpMethod.Post, request.Method);
        Assert.AreEqual("https://cloud.example/payments/card/start", request.Uri.AbsoluteUri);
        Assert.AreEqual(ApiKey, request.Headers["X-Api-Key"]);
        using var body = JsonDocument.Parse(request.Body!);
        Assert.AreEqual(order.RequestId, body.RootElement.GetProperty("RequestId").GetGuid());
        Assert.AreEqual("NON_CASH", body.RootElement.GetProperty("PaymentType").GetString());
    }

    [DataRow("https://cloud.example")]
    [DataRow("https://cloud.example/")]
    [TestMethod]
    public async Task Calls_WorkWithAndWithoutATrailingSlashInTheConfiguredUrl(string url)
    {
        using var host = await Host.CreateAsync(url);
        host.Handler.Enqueue(HttpStatusCode.OK, StatusJson(Guid.NewGuid(), PaymentRequestStatus.Pending));

        await host.Client.GetCardPaymentAsync(Guid.NewGuid());

        StringAssert.StartsWith(host.Handler.Requests.Single().Uri.AbsoluteUri, "https://cloud.example/payments/card/");
    }

    [TestMethod]
    public async Task Calls_KeepAPathInTheConfiguredUrl()
    {
        using var host = await Host.CreateAsync("https://example.com/innkeep");
        var id = Guid.NewGuid();
        host.Handler.Enqueue(HttpStatusCode.OK, StatusJson(id, PaymentRequestStatus.Pending));

        await host.Client.GetCardPaymentAsync(id);

        Assert.AreEqual($"https://example.com/innkeep/payments/card/{id}", host.Handler.Requests.Single().Uri.AbsoluteUri);
    }

    [TestMethod]
    public async Task Get_RequestsTheStatusOfThePayment()
    {
        using var host = await Host.CreateAsync();
        var id = Guid.NewGuid();
        host.Handler.Enqueue(HttpStatusCode.OK, StatusJson(id, PaymentRequestStatus.Pending));

        await host.Client.GetCardPaymentAsync(id);

        var request = host.Handler.Requests.Single();
        Assert.AreEqual(HttpMethod.Get, request.Method);
        Assert.AreEqual($"https://cloud.example/payments/card/{id}", request.Uri.AbsoluteUri);
        Assert.AreEqual(ApiKey, request.Headers["X-Api-Key"]);
    }

    [TestMethod]
    public async Task Cancel_PostsToTheCancelUrlWithoutABody()
    {
        using var host = await Host.CreateAsync();
        var id = Guid.NewGuid();
        host.Handler.Enqueue(HttpStatusCode.OK, StatusJson(id, PaymentRequestStatus.Pending));

        var result = await host.Client.CancelCardPaymentAsync(id);

        Assert.IsTrue(result.IsSuccess);
        var request = host.Handler.Requests.Single();
        Assert.AreEqual(HttpMethod.Post, request.Method);
        Assert.AreEqual($"https://cloud.example/payments/card/{id}/cancel", request.Uri.AbsoluteUri);
    }

    [DataRow(PaymentRequestStatus.Pending)]
    [DataRow(PaymentRequestStatus.Paid)]
    [DataRow(PaymentRequestStatus.Completed)]
    [DataRow(PaymentRequestStatus.Failed)]
    [DataRow(PaymentRequestStatus.Cancelled)]
    [DataRow(PaymentRequestStatus.Expired)]
    [TestMethod]
    public async Task Get_ReadsEveryStatusFromTheCloudJson(PaymentRequestStatus status)
    {
        using var host = await Host.CreateAsync();
        var id = Guid.NewGuid();
        host.Handler.Enqueue(HttpStatusCode.OK, StatusJson(id, status, "message"));

        var result = await host.Client.GetCardPaymentAsync(id);

        Assert.AreEqual(id, result.Value!.RequestId);
        Assert.AreEqual(status, result.Value.Status);
        Assert.AreEqual("message", result.Value.Message);
    }

    [TestMethod]
    public async Task Get_ReadsTheReceiptOfACompletedPayment()
    {
        using var host = await Host.CreateAsync();
        var id = Guid.NewGuid();
        host.Handler.Enqueue(HttpStatusCode.OK, StatusJson(id, PaymentRequestStatus.Completed, receipt: BuildReceipt()));

        var result = await host.Client.GetCardPaymentAsync(id);

        Assert.AreEqual("ABC12", result.Value!.Receipt!.PretixOrderCode);
        Assert.AreEqual(PaymentType.NonCash, result.Value.Receipt.PaymentType);
        Assert.AreEqual(20m, result.Value.Receipt.Sum.TotalAmount);
    }

    [TestMethod]
    public async Task Errors_KeepTheStatusCodeAndTheBodyOfTheCloud()
    {
        using var host = await Host.CreateAsync();
        const string body = """{"code":"Payment.ReaderBusy","message":"Der Kartenleser ist gerade in Benutzung"}""";
        host.Handler.Enqueue(HttpStatusCode.BadRequest, body);

        var result = await host.Client.StartCardPaymentAsync(BuildOrder());

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("Http.400", result.Error!.Code);
        StringAssert.Contains(result.Error.Message, "Payment.ReaderBusy");
    }

    [TestMethod]
    public async Task WithoutAnActiveApiKey_NothingIsSentAndTheCallFails()
    {
        using var host = await Host.CreateAsync(url: null);

        var result = await host.Client.StartCardPaymentAsync(BuildOrder());

        Assert.IsFalse(result.IsSuccess);
        Assert.IsEmpty(host.Handler.Requests);
        Assert.AreEqual("Http.503", result.Error!.Code);
    }

    [TestMethod]
    public async Task ServerService_ReportsACloudBusyReaderWithTheOriginalMessage()
    {
        using var host = await Host.CreateAsync();
        host.Handler.Enqueue(HttpStatusCode.BadRequest, """{"code":"Payment.ReaderBusy","message":"Der Kartenleser ist gerade in Benutzung"}""");

        var result = await new ServerPaymentService(host.Client).StartAsync(BuildOrder());

        Assert.AreEqual("Payment.ReaderBusy", result.Error!.Code);
        Assert.AreEqual("Der Kartenleser ist gerade in Benutzung", result.Error.Message);
    }

    [TestMethod]
    public async Task ServerService_ReportsAnUnknownPaymentAsNotFound()
    {
        using var host = await Host.CreateAsync();
        host.Handler.Enqueue(HttpStatusCode.NotFound, """{"code":"Payment.NotFound","message":"Die Zahlung wurde nicht gefunden"}""");

        var result = await new ServerPaymentService(host.Client).GetStatusAsync(Guid.NewGuid());

        Assert.AreEqual("Payment.NotFound", result.Error!.Code);
        Assert.AreEqual(404, ServerPaymentService.StatusCodeFor(result.Error));
    }

    [DataRow(HttpStatusCode.BadGateway)]
    [DataRow(HttpStatusCode.ServiceUnavailable)]
    [DataRow(HttpStatusCode.GatewayTimeout)]
    [TestMethod]
    public async Task ServerService_ReportsAProxyErrorAsCloudUnreachable(HttpStatusCode statusCode)
    {
        using var host = await Host.CreateAsync();
        host.Handler.Enqueue(statusCode, "<html>proxy error</html>");

        var result = await new ServerPaymentService(host.Client).GetStatusAsync(Guid.NewGuid());

        Assert.AreEqual(ServerPaymentService.CloudUnreachableCode, result.Error!.Code);
    }

    [TestMethod]
    public async Task ServerService_ReportsANetworkFailureAsCloudUnreachable()
    {
        using var host = await Host.CreateAsync();
        host.Handler.EnqueueException(new HttpRequestException("connection refused"));

        var result = await new ServerPaymentService(host.Client).GetStatusAsync(Guid.NewGuid());

        Assert.AreEqual(ServerPaymentService.CloudUnreachableCode, result.Error!.Code);
    }

    [TestMethod]
    public async Task ServerService_ReportsAMissingCredentialAsCloudUnreachable()
    {
        using var host = await Host.CreateAsync(url: null);

        var result = await new ServerPaymentService(host.Client).StartAsync(BuildOrder());

        Assert.AreEqual(ServerPaymentService.CloudUnreachableCode, result.Error!.Code);
    }
}
