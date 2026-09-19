using Innkeep2.Models.Core;
using Innkeep2.Models.Fiskaly.Transaction;
using Innkeep2.Models.Internal;
using Innkeep2.Requests.Fiskaly;

namespace Innkeep2.Services.Cloud.Fiskaly;

public sealed class FiskalyTransactionService(
   FiskalyTransactionClient transactionClient,
   IActiveConfigurationService activeConfiguration
)
{
    public Task<Result<FiskalyTransaction>> StartAsync(Guid txId, CancellationToken ct = default)
    {
       if (activeConfiguration.Tss is not { } tss || activeConfiguration.Client is not { } client)
          return Task.FromResult(Result<FiskalyTransaction>.Failure(
             new Error("Transaction.NoConfiguration", "No TSS or client is currently selected.")));

       return transactionClient.StartAsync(tss.Id, txId, client.Id, ct);
    }

    public Task<Result<FiskalyTransaction>> FinishAsync(
       Guid txId,
       int revision,
       OrderRequest order,
       CancellationToken ct = default
    )
    {
       if (activeConfiguration.Tss is not { } tss || activeConfiguration.Client is not { } client)
          return Task.FromResult(Result<FiskalyTransaction>.Failure(
             new Error("Transaction.NoConfiguration", "No TSS or client is currently selected.")));

       var schema = BuildSchema(order);

       return transactionClient.FinishAsync(tss.Id, txId, client.Id, revision, schema, ct);
    }

    private static FiskalyTransactionSchema BuildSchema(OrderRequest order)
    {
       var vatRates = order.Items
          .GroupBy(x => x.TaxRate)
          .Select(g => new FiskalyAmountPerVatRate
          {
             VatRate = VatRateMapper.FromTaxRate(g.Key),
             Amount = g.Sum(x => x.Price * (x.Quantity ?? 1))
          })
          .ToList();

       var paymentTypes = new List<FiskalyAmountPerPaymentType>
       {
          new()
          {
             PaymentType = order.PaymentType,
             Amount = order.AmountNeeded,
             CurrencyCode = order.Currency
          }
       };

       return new FiskalyTransactionSchema
       {
          StandardV1 = new FiskalyStandardV1Schema
          {
             Receipt = new FiskalyReceipt
             {
                AmountsPerVatRate = vatRates,
                AmountsPerPaymentType = paymentTypes
             }
          }
       };
    }
}