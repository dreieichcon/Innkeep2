using Innkeep2.Models.Fiskaly.Transaction;
using Innkeep2.Models.Shared;
using Innkeep2.Requests.Fiskaly;
using Microsoft.Extensions.DependencyInjection;

namespace Innkeep2.Requests.Tests.Integration.Fiskaly;

[TestClass]
public class FiskalyTransactionClientIntegrationTests : FiskalyTssIntegrationTestBase
{
    [TestMethod]
    public async Task StartAsync_ReturnsActiveTransaction()
    {
        var client = ServiceProvider.GetRequiredService<FiskalyTransactionClient>();
        var txId = Guid.NewGuid();

        var result = await client.StartAsync(TssId, txId, ClientId);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(TxState.Active, result.Value!.State);
    }

    [TestMethod]
    public async Task FinishAsync_ReturnsFinishedTransactionWithQrCode()
    {
        var client = ServiceProvider.GetRequiredService<FiskalyTransactionClient>();
        var txId = Guid.NewGuid();

        var startResult = await client.StartAsync(TssId, txId, ClientId);
        Assert.IsTrue(startResult.IsSuccess);

        var schema = new FiskalyTransactionSchema
        {
            StandardV1 = new FiskalyStandardV1Schema
            {
                Receipt = new FiskalyReceipt
                {
                    AmountsPerVatRate =
                    [
                        new FiskalyAmountPerVatRate { VatRate = VatRate.Normal, Amount = 10.00m }
                    ],
                    AmountsPerPaymentType =
                    [
                        new FiskalyAmountPerPaymentType { PaymentType = PaymentType.Cash, Amount = 10.00m, CurrencyCode = "EUR" }
                    ]
                }
            }
        };

        var finishResult = await client.FinishAsync(TssId, txId, ClientId, revision: 2, schema);

        Assert.IsTrue(finishResult.IsSuccess);
        Assert.AreEqual(TxState.Finished, finishResult.Value!.State);
        Assert.IsFalse(string.IsNullOrWhiteSpace(finishResult.Value!.QrCodeData));
    }
    
    [TestMethod]
    public async Task FinishAsync_WithNegativeAmount_ReturnsFinishedTransaction()
    {
        var client = ServiceProvider.GetRequiredService<FiskalyTransactionClient>();
        var txId = Guid.NewGuid();

        var startResult = await client.StartAsync(TssId, txId, ClientId);
        Assert.IsTrue(startResult.IsSuccess);

        var schema = new FiskalyTransactionSchema
        {
            StandardV1 = new FiskalyStandardV1Schema
            {
                Receipt = new FiskalyReceipt
                {
                    AmountsPerVatRate =
                    [
                        new FiskalyAmountPerVatRate { VatRate = VatRate.Normal, Amount = -10.00m }
                    ],
                    AmountsPerPaymentType =
                    [
                        new FiskalyAmountPerPaymentType { PaymentType = PaymentType.Cash, Amount = -10.00m, CurrencyCode = "EUR" }
                    ]
                }
            }
        };

        var finishResult = await client.FinishAsync(TssId, txId, ClientId, revision: 2, schema);

        Assert.IsTrue(finishResult.IsSuccess);
        Assert.AreEqual(TxState.Finished, finishResult.Value!.State);
    }
}