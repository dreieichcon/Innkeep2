using Innkeep2.Models.Internal;
using Innkeep2.Models.Internal.Receipt;
using Innkeep2.Models.Shared;
using Innkeep2.Requests.Cloud;
using Innkeep2.Server.Api;
using Innkeep2.Server.Extensions;
using Innkeep2.Services.Server;
using Innkeep2.TestBase;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Innkeep2.Server.Tests.Integration.Orders;

[TestClass]
public class OrderHandlersIntegrationTests
{
    private ServiceProvider _serviceProvider = null!;

    [TestInitialize]
    public void TestInitialize()
    {
       var credentialsPath = CredentialsPathResolver.ResolveCredentialsPath("server.");

       var configuration = new ConfigurationBuilder()
          .AddJsonFile(credentialsPath, optional: false)
          .AddEnvironmentVariables()
          .Build();

       var services = new ServiceCollection();
       services.AddCloudCredential(configuration);
       services.AddCloudClients();
       services.AddMemoryCache();
       services.AddSingleton<ServerEventProvider>();
       services.AddSingleton<ServerSalesItemProvider>();

       _serviceProvider = services.BuildServiceProvider();
    }

    [TestCleanup]
    public void TestCleanup() => _serviceProvider?.Dispose();

    [TestMethod]
    public async Task CreateOrderAsync_CloudReachable_ReturnsRealReceipt()
    {
       var cloudClient = _serviceProvider.GetRequiredService<CloudTransactionClient>();
       var eventProvider = _serviceProvider.GetRequiredService<ServerEventProvider>();
       var queue = new FakeRequestQueueRepository();

       var eventResult = await eventProvider.GetCachedEventAsync();
       Assert.IsTrue(eventResult.IsSuccess);

       var salesItemProvider = _serviceProvider.GetRequiredService<ServerSalesItemProvider>();
       var salesItemsResult = await salesItemProvider.GetCachedItemsAsync();
       var item = salesItemsResult.Value!.First();
       item.Quantity = 1;

       var request = new OrderRequest
       {
          RequestId = Guid.NewGuid(),
          Items = [item],
          PaymentType = PaymentType.Cash,
          AmountGiven = item.Price,
          Currency = "EUR"
       };

       var result = await OrderHandlers.CreateOrderAsync(request, cloudClient, eventProvider, queue, CancellationToken.None);

       var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<TransactionReceipt>;
       Assert.IsNotNull(okResult);
       Assert.AreNotEqual("PRETIX OFFLINE", okResult.Value!.PretixOrderCode);
       Assert.AreNotEqual("TSS OFFLINE", okResult.Value!.FiskalyQrCode);
       Assert.IsFalse(queue.WasEnqueued(request.RequestId));
    }
}