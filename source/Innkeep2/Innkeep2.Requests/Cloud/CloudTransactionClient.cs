using System.Text.Json;
using Innkeep2.Models.Core;
using Innkeep2.Models.Internal;
using Innkeep2.Models.Internal.Receipt;
using Innkeep2.Requests.Core;

namespace Innkeep2.Requests.Cloud;

public sealed class CloudTransactionClient(HttpClient httpClient)
    : CoreApiClient(httpClient, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
{
    public Task<Result<TransactionReceipt>> CreateOrderAsync(OrderRequest request, CancellationToken ct = default)
        => PostAsync<TransactionReceipt>("orders/create", request, ct);

    public Task<Result<TransactionReceipt>> RefundAsync(Guid requestId, CancellationToken ct = default)
        => PostAsync<TransactionReceipt>($"transactions/{requestId}/refund", null, ct);

    public Task<Result<TransactionReceipt>> TransferAsync(TransferRequest request, CancellationToken ct = default)
        => PostAsync<TransactionReceipt>("transactions/transfer", request, ct);
}