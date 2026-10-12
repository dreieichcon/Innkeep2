using Innkeep2.Cloud.TransactionDb.Repositories;
using Innkeep2.Models.Fiskaly.Client;
using Innkeep2.Models.Fiskaly.Tss;
using Innkeep2.Models.Internal;
using Innkeep2.Models.Shared;
using Innkeep2.Requests.Fiskaly;
using Innkeep2.Requests.Pretix;
using Innkeep2.Requests.Pretix.Clients;
using Innkeep2.Services.Cloud;
using Innkeep2.Services.Cloud.Cache;
using Innkeep2.Services.Cloud.Fiskaly;
using Innkeep2.Services.Cloud.Pretix;
using Innkeep2.TestBase;
using Microsoft.Extensions.DependencyInjection;
using TransactionService = Innkeep2.Cloud.Services.Transactions.TransactionService;

namespace Innkeep2.Cloud.Tests.Integration.Orders;

[TestClass]
public class TransactionServiceIntegrationTests
{
    private ServiceProvider _serviceProvider = null!;
    private FakeActiveConfigurationService _activeConfiguration = null!;

    [TestInitialize]
    public async Task TestInitialize()
    {

       var services = new ServiceCollection();
       services.AddDevCredentialPair();
       services.AddPretixClients();
       services.AddFiskalyClients();
       services.AddMemoryCache();
       services.AddSingleton<CachedSalesItemProvider>();
       services.AddSingleton<PretixOrderService>();
       services.AddSingleton<FiskalyTransactionService>();
       services.AddSingleton<TransactionRepository, FakeTransactionRepository>();

       _activeConfiguration = new FakeActiveConfigurationService();
       services.AddSingleton<IActiveConfigurationService>(_activeConfiguration);

       services.AddSingleton<TransactionService>();

       _serviceProvider = services.BuildServiceProvider();

       ResolveActiveConfiguration(TestData.Load());
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
    
    [TestMethod]
    public async Task CreateOrderAsync_ThenRefundTransactionAsync_ReturnsRefundReceipt()
    {
       var transactionService = _serviceProvider.GetRequiredService<TransactionService>();
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

       var saleResult = await transactionService.CreateOrderAsync(request);
       Assert.IsTrue(saleResult.IsSuccess);

       var refundId = Guid.NewGuid();
       var refundResult = await transactionService.RefundTransactionAsync(request.RequestId, refundId);

       Assert.IsTrue(refundResult.IsSuccess);
       Assert.AreEqual(refundId, refundResult.Value!.OrderId);
       Assert.AreEqual(request.RequestId, refundResult.Value!.RefundRequestId);

       Assert.IsTrue(refundResult.IsSuccess);
       Assert.AreEqual(0, refundResult.Value!.Sum.AmountGiven);
       Assert.AreEqual(item.Price, refundResult.Value!.Sum.AmountReturned);
       Assert.AreNotEqual("TSS OFFLINE", refundResult.Value!.FiskalyQrCode);
       Assert.AreNotEqual("PRETIX OFFLINE", refundResult.Value!.PretixOrderCode);
    }
    
    [TestMethod]
    public async Task CreateTransferAsync_InboundTransfer_ReturnsReceiptWithGivenAmount()
    {
       var transactionService = _serviceProvider.GetRequiredService<TransactionService>();

       var request = new TransferRequest
       {
          RequestId = Guid.NewGuid(),
          Amount = 50.00m,
          Currency = "EUR"
       };

       var result = await transactionService.CreateTransferAsync(request);

       Assert.IsTrue(result.IsSuccess);
       Assert.AreEqual(50.00m, result.Value!.Sum.AmountGiven);
       Assert.AreEqual(0, result.Value!.Sum.AmountReturned);
       Assert.AreNotEqual("TSS OFFLINE", result.Value!.FiskalyQrCode);
    }

    [TestMethod]
    public async Task CreateTransferAsync_OutboundTransfer_ReturnsReceiptWithReturnedAmount()
    {
       var transactionService = _serviceProvider.GetRequiredService<TransactionService>();

       var request = new TransferRequest
       {
          RequestId = Guid.NewGuid(),
          Amount = -20.00m,
          Currency = "EUR"
       };

       var result = await transactionService.CreateTransferAsync(request);

       Assert.IsTrue(result.IsSuccess);
       Assert.AreEqual(0, result.Value!.Sum.AmountGiven);
       Assert.AreEqual(20.00m, result.Value!.Sum.AmountReturned);
       Assert.AreNotEqual("TSS OFFLINE", result.Value!.FiskalyQrCode);
    }

    private void ResolveActiveConfiguration(TestData testData)
    {
       _activeConfiguration.Organizer = new Organizer
       {
          Name = "",
          Slug = testData.PretixOrganizerSlug
       };

       _activeConfiguration.Event = new Event
       {
          Name = "",
          Slug = testData.PretixEventSlug,
          IsTestMode = true
       };

       _activeConfiguration.Tss = new FiskalyTss
       {
          Id = testData.FiskalyTssId,
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
          Id = testData.FiskalyClientId,
          TssId = _activeConfiguration.Tss.Id,
          SerialNumber = "",
          State = ClientState.Registered,
          TimeCreation = 0
       };
    }
}