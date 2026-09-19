using Innkeep2.Cloud.Services;
using Innkeep2.Cloud.TransactionDb.Repositories;
using Innkeep2.Credentials;
using Innkeep2.Models.Fiskaly.Client;
using Innkeep2.Models.Fiskaly.Tss;
using Innkeep2.Models.Internal;
using Innkeep2.Models.Shared;
using Innkeep2.Requests.Fiskaly;
using Innkeep2.Requests.Pretix;
using Innkeep2.Requests.Pretix.Clients;
using Innkeep2.Services.Cloud;
using Innkeep2.Services.Cloud.Fiskaly;
using Innkeep2.Services.Cloud.Pretix;
using Innkeep2.TestBase;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Innkeep2.Cloud.Tests.Integration.Orders;

[TestClass]
public class OrderServiceIntegrationTests
{
    private ServiceProvider _serviceProvider = null!;
    private FakeActiveConfigurationService _activeConfiguration = null!;

    [TestInitialize]
    public async Task TestInitialize()
    {
       var credentialsPath = CredentialsPathResolver.ResolveCredentialsPath();

       var configuration = new ConfigurationBuilder()
          .AddJsonFile(credentialsPath, optional: false)
          .AddEnvironmentVariables()
          .Build();

       var services = new ServiceCollection();
       services.AddCredentialsConfiguration(configuration);
       services.AddSingleton<IConfiguration>(configuration);
       services.AddPretixClients();
       services.AddFiskalyClients();
       services.AddSingleton<PretixOrderService>();
       services.AddSingleton<FiskalyTransactionService>();
       services.AddSingleton<TransactionRepository, FakeTransactionRepository>();

       _activeConfiguration = new FakeActiveConfigurationService();
       services.AddSingleton<IActiveConfigurationService>(_activeConfiguration);

       services.AddSingleton<TransactionService>();

       _serviceProvider = services.BuildServiceProvider();

       ResolveActiveConfiguration(configuration);
    }

    [TestCleanup]
    public void TestCleanup() => _serviceProvider?.Dispose();

    [TestMethod]
    public async Task CreateOrderAsync_ReturnsReceiptWithCompletedSteps()
    {
       var orderService = _serviceProvider.GetRequiredService<TransactionService>();
       var salesItemClient = _serviceProvider.GetRequiredService<PretixSalesItemClient>();

       var salesItems = await salesItemClient.GetAllAsync(_activeConfiguration.Organizer!.Slug, _activeConfiguration.Event!.Slug);
       var item = SalesItem.FromPretix(salesItems.Value!.Results.First()).First();
       item.Quantity = 1;

       var request = new OrderRequest
       {
          RequestId = Guid.NewGuid(),
          Items = [item],
          PaymentType = PaymentType.Cash,
          AmountGiven = item.Price,
          Currency = "EUR"
       };

       var result = await orderService.CreateOrderAsync(request);

       Assert.IsTrue(result.IsSuccess);
       Assert.AreEqual(request.RequestId, result.Value!.OrderId);
       Assert.AreNotEqual("PRETIX OFFLINE", result.Value!.PretixOrderCode);
       Assert.AreNotEqual("TSS OFFLINE", result.Value!.FiskalyQrCode);
    }
    
    [TestMethod]
    public async Task CreateOrderAsync_WithVoucherItem_ReturnsReceiptWithVoucher()
    {
       var orderService = _serviceProvider.GetRequiredService<TransactionService>();
       var salesItemClient = _serviceProvider.GetRequiredService<PretixSalesItemClient>();

       var salesItems = await salesItemClient.GetAllAsync(_activeConfiguration.Organizer!.Slug, _activeConfiguration.Event!.Slug);
       var item = SalesItem.FromPretix(salesItems.Value!.Results.First()).First();
       item.Quantity = 1;
       item.PrintCheckInVoucher = true;

       var request = new OrderRequest
       {
          RequestId = Guid.NewGuid(),
          Items = [item],
          PaymentType = PaymentType.Cash,
          AmountGiven = item.Price,
          Currency = "EUR"
       };

       var result = await orderService.CreateOrderAsync(request);

       Assert.IsTrue(result.IsSuccess);
       Assert.HasCount(1, result.Value!.Vouchers);
       Assert.AreEqual(item.Name, result.Value!.Vouchers[0].ItemName);
       Assert.IsFalse(string.IsNullOrWhiteSpace(result.Value!.Vouchers[0].Secret));
    }

    private void ResolveActiveConfiguration(IConfiguration configuration)
    {
       _activeConfiguration.Organizer = new Organizer
       {
          Name = "",
          Slug = configuration["pretixTest:organizerSlug"]!
       };

       _activeConfiguration.Event = new Event
       {
          Name = "",
          Slug = configuration["pretixTest:eventSlug"]!,
          IsTestMode = true
       };

       _activeConfiguration.Tss = new FiskalyTss
       {
          Id = Guid.Parse(configuration["fiskalyTest:tssId"]!),
          Environment = "TEST",
          State = TssState.Initialized,
          Certificate = "",
          SerialNumber = "",
          PublicKey = "",
          MaxNumberRegisteredClients = 0,
          MaxNumberActiveTransactions = 0,
          TimeCreation = 0
       };

       _activeConfiguration.Client = new FiskalyClient
       {
          Id = Guid.Parse(configuration["fiskalyTest:clientId"]!),
          TssId = _activeConfiguration.Tss.Id,
          SerialNumber = "",
          State = ClientState.Registered,
          TimeCreation = 0
       };
    }
}