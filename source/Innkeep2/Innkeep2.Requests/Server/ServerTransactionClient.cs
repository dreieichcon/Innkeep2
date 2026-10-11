using System.Text.Json;
using Demolite.Core.Models;
using Demolite.Http.Client;
using Innkeep2.Models.Internal;
using Innkeep2.Models.Internal.Receipt;
using Microsoft.Extensions.Logging;

namespace Innkeep2.Requests.Server;

public sealed class ServerTransactionClient(HttpClient httpClient, ILogger<ServerTransactionClient> logger)
    : ApiClient(httpClient, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, logger)
{
    public Task<Result<TransactionReceipt>> CreateOrderAsync(OrderRequest request, CancellationToken ct = default)
        => PostAsync<TransactionReceipt>("orders/create", request, ct);
}
