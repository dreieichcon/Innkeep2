using Demolite.Core.Models;
using Innkeep2.Models.Internal;
using Innkeep2.Models.Internal.Receipt;

namespace Innkeep2.Cloud.Services.Transactions;

public interface IOrderCreationService
{
    Task<Result<TransactionReceipt>> CreateOrderAsync(OrderRequest request, CancellationToken ct = default, bool isRetry = false, string? sumupTransactionCode = null);

    Task<TransactionReceipt?> GetReceiptAsync(Guid requestId, CancellationToken ct = default);
}
