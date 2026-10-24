using System.Net;
using System.Text.Json;
using Demolite.Core.Models;
using Innkeep2.CredentialsDb;
using Innkeep2.CredentialsDb.Interfaces;
using Innkeep2.CredentialsDb.Models;
using Innkeep2.Models.Internal;
using Innkeep2.Models.Shared;
using Innkeep2.Requests.Server;
using Innkeep2.Services.Extensions;
using Innkeep2.Services.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Innkeep2.Services.Tests.Client;

[TestClass]
public class ServerPaymentClientTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private sealed class Host : IDisposable
    {
        private readonly ServiceProvider _provider;

        private Host(string? url)
        {
            var repository = Substitute.For<ICredentialRepository<InnkeepApiKey>>();
            repository.GetActiveAsync().Returns(url is null
                ? Result<InnkeepApiKey>.Failure(new Error("NotFound", "none"))
                : Result<InnkeepApiKey>.Success(new InnkeepApiKey { Name = "Server", Active = true, ApiKey = "client-key", Url = url }));

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(repository);
            services.AddSingleton<ActiveCredentialProvider<InnkeepApiKey>>();
            services.AddInnkeepApiClient<ServerPaymentClient>(timeoutSeconds: 15);
            services.AddHttpClient<ServerPaymentClient>().ConfigurePrimaryHttpMessageHandler(() => Handler);
            _provider = services.BuildServiceProvider();
        }

        public StubHttpMessageHandler Handler { get; } = new();

        public ServerPaymentClient Client => _provider.GetRequiredService<ServerPaymentClient>();

        public static async Task<Host> CreateAsync(string? url = "https://server.example/api/v1")
        {
            var host = new Host(url);
            await host._provider.GetRequiredService<ActiveCredentialProvider<InnkeepApiKey>>().ReloadAsync();

            return host;
        }

        public void Dispose() => _provider.Dispose();
    }

    private static string StatusJson(Guid id, PaymentRequestStatus status, string? message = null)
        => JsonSerializer.Serialize(new PaymentStatusResponse(id, status, message), Web);

    private static OrderRequest BuildOrder() => new()
    {
        RequestId = Guid.NewGuid(),
        Items = [new SalesItem { Id = 1, VariationId = 0, Name = "Item", Price = 10m, TaxRate = 19m, Quantity = 1 }],
        PaymentType = PaymentType.NonCash,
        AmountGiven = 10m,
        Currency = "EUR"
    };

    [TestMethod]
    public async Task Start_PostsTheOrderToTheServerWithTheApiKey()
    {
        using var host = await Host.CreateAsync();
        var order = BuildOrder();
        host.Handler.Enqueue(HttpStatusCode.OK, StatusJson(order.RequestId, PaymentRequestStatus.Pending, "Bitte am Kartenleser bezahlen"));

        var result = await host.Client.StartCardPaymentAsync(order);

        var request = host.Handler.Requests.Single();
        Assert.AreEqual(HttpMethod.Post, request.Method);
        Assert.AreEqual("https://server.example/api/v1/payments/card/start", request.Uri.AbsoluteUri);
        Assert.AreEqual("client-key", request.Headers["X-Api-Key"]);
        Assert.AreEqual(PaymentRequestStatus.Pending, result.Value!.Status);
        using var body = JsonDocument.Parse(request.Body!);
        Assert.AreEqual("NON_CASH", body.RootElement.GetProperty("PaymentType").GetString());
    }

    [TestMethod]
    public async Task Get_RequestsTheStatusFromTheServer()
    {
        using var host = await Host.CreateAsync();
        var id = Guid.NewGuid();
        host.Handler.Enqueue(HttpStatusCode.OK, StatusJson(id, PaymentRequestStatus.Paid));

        var result = await host.Client.GetCardPaymentAsync(id);

        var request = host.Handler.Requests.Single();
        Assert.AreEqual(HttpMethod.Get, request.Method);
        Assert.AreEqual($"https://server.example/api/v1/payments/card/{id}", request.Uri.AbsoluteUri);
        Assert.AreEqual(PaymentRequestStatus.Paid, result.Value!.Status);
    }

    [TestMethod]
    public async Task Cancel_PostsToTheCancelUrl()
    {
        using var host = await Host.CreateAsync();
        var id = Guid.NewGuid();
        host.Handler.Enqueue(HttpStatusCode.OK, StatusJson(id, PaymentRequestStatus.Pending));

        var result = await host.Client.CancelCardPaymentAsync(id);

        Assert.IsTrue(result.IsSuccess);
        var request = host.Handler.Requests.Single();
        Assert.AreEqual(HttpMethod.Post, request.Method);
        Assert.AreEqual($"https://server.example/api/v1/payments/card/{id}/cancel", request.Uri.AbsoluteUri);
    }

    [DataRow(HttpStatusCode.BadRequest, "Http.400")]
    [DataRow(HttpStatusCode.NotFound, "Http.404")]
    [DataRow(HttpStatusCode.ServiceUnavailable, "Http.503")]
    [DataRow(HttpStatusCode.BadGateway, "Http.502")]
    [TestMethod]
    public async Task Errors_KeepTheStatusCodeAndTheErrorBodyOfTheServer(HttpStatusCode statusCode, string expectedCode)
    {
        using var host = await Host.CreateAsync();
        host.Handler.Enqueue(statusCode, """{"code":"Payment.ReaderBusy","message":"Der Kartenleser ist gerade in Benutzung"}""");

        var result = await host.Client.StartCardPaymentAsync(BuildOrder());

        Assert.AreEqual(expectedCode, result.Error!.Code);
        StringAssert.Contains(result.Error.Message, "Der Kartenleser ist gerade in Benutzung");
    }

    [TestMethod]
    public async Task Errors_ReportANetworkFailure()
    {
        using var host = await Host.CreateAsync();
        host.Handler.EnqueueException(new HttpRequestException("connection refused"));

        var result = await host.Client.GetCardPaymentAsync(Guid.NewGuid());

        Assert.AreEqual("Http.RequestFailed", result.Error!.Code);
    }

    [TestMethod]
    public async Task WithoutAnActiveApiKey_NothingIsSent()
    {
        using var host = await Host.CreateAsync(url: null);

        var result = await host.Client.StartCardPaymentAsync(BuildOrder());

        Assert.IsFalse(result.IsSuccess);
        Assert.IsEmpty(host.Handler.Requests);
    }
}
