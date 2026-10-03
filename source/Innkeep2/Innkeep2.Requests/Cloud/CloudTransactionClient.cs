using System.Text.Json;
using Innkeep2.Models.Core;
using Innkeep2.Models.Internal;
using Innkeep2.Models.Internal.Receipt;
using Innkeep2.Requests.Core;

namespace Innkeep2.Requests.Cloud;

public sealed class CloudTransactionClient(HttpClient httpClient)
    : CoreApiClient(httpClient, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
{
    public Task<Result<TransactionReceipt>> CreateOrderAsync(OrderRequest request, CancellationToken ct = default, bool isRetry = false)
        => PostAsync<TransactionReceipt>($"transactions/create{RetryQuery(isRetry)}", request, ct);

    public Task<Result<TransactionReceipt>> RefundAsync(
        Guid requestId,
        Guid refundRequestId,
        CancellationToken ct = default,
        bool isRetry = false
    )
        => PostAsync<TransactionReceipt>($"transactions/{requestId}/refund/{refundRequestId}{RetryQuery(isRetry)}", null, ct);

    public Task<Result<TransactionReceipt>> TransferAsync(TransferRequest request, CancellationToken ct = default, bool isRetry = false)
        => PostAsync<TransactionReceipt>($"transactions/transfer{RetryQuery(isRetry)}", request, ct);

    private static string RetryQuery(bool isRetry) => isRetry ? "?retry=true" : "";
    
    public Task<Result<PagedResult<TransactionSummary>>> GetTransactionsAsync(
        int skip,
        int take,
        CancellationToken ct = default
    )
        => GetAsync<PagedResult<TransactionSummary>>($"transactions?skip={skip}&take={take}", ct);

    public Task<Result<TransactionReceipt>> GetTransactionAsync(Guid requestId, CancellationToken ct = default)
        => GetAsync<TransactionReceipt>($"transactions/{requestId}", ct);
}