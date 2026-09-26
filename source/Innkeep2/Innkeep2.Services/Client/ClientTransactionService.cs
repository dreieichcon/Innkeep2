using Innkeep2.Models.Core;
using Innkeep2.Models.Internal;
using Innkeep2.Models.Internal.Receipt;
using Innkeep2.Models.Shared;
using Innkeep2.Requests.Server;

namespace Innkeep2.Services.Client;

public sealed class ClientTransactionService(ServerTransactionClient transactionClient, ClientCartService cartService)
{
    public decimal AmountGiven { get; private set; }

    public decimal AmountBack => AmountGiven - cartService.Total;

    public TransactionReceipt? LastReceipt { get; private set; }

    public event EventHandler? Changed;

    public void SetAmountGiven(decimal amount)
    {
        AmountGiven = amount;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public async Task<Result<TransactionReceipt>> SubmitOrderAsync(CancellationToken ct = default)
    {
        var request = new OrderRequest
        {
            RequestId = Guid.NewGuid(),
            Items = cartService.Items,
            PaymentType = PaymentType.Cash,
            AmountGiven = AmountGiven,
            Currency = "EUR"
        };

        var result = await transactionClient.CreateOrderAsync(request, ct);

        if (result.IsSuccess)
            LastReceipt = result.Value;

        return result;
    }
}