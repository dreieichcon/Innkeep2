using System.Text.Json;
using Demolite.Core.Models;
using Innkeep2.Models.Internal;
using Innkeep2.Models.Internal.Receipt;
using Innkeep2.Requests.Core;

namespace Innkeep2.Requests.Server;

public sealed class ServerTransactionClient(HttpClient httpClient)
    : CoreApiClient(httpClient, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
{
    public Task<Result<TransactionReceipt>> CreateOrderAsync(OrderRequest request, CancellationToken ct = default)
        => PostAsync<TransactionReceipt>("orders/create", request, ct);
}
