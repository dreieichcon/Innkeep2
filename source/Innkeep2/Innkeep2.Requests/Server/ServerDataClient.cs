using System.Text.Json;
using Demolite.Core.Models;
using Demolite.Http.Client;
using Innkeep2.Models.Internal;
using Microsoft.Extensions.Logging;

namespace Innkeep2.Requests.Server;

public sealed class ServerDataClient(HttpClient httpClient, ILogger<ServerDataClient> logger)
    : ApiClient(httpClient, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, logger)
{
    public Task<Result<Event>> GetEventAsync(CancellationToken ct = default)
        => GetAsync<Event>("data/event", ct);

    public Task<Result<IReadOnlyList<SalesItem>>> GetSalesItemsAsync(CancellationToken ct = default)
        => GetAsync<IReadOnlyList<SalesItem>>("data/salesitems", ct);

    public Task<Result<SalesItemsResult>> RefreshSalesItemsAsync(CancellationToken ct = default)
        => PostAsync<SalesItemsResult>("data/salesitems/refresh", null, ct);

    public Task<Result<SalesItemsResult>> RefreshSalesItemStockAsync(CancellationToken ct = default)
        => PostAsync<SalesItemsResult>("data/salesitems/stock/refresh", null, ct);
}
