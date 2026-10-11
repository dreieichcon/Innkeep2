using System.Text.Json;
using Demolite.Core.Models;
using Innkeep2.Models.Internal;
using Innkeep2.Requests.Core;

namespace Innkeep2.Requests.Server;

public sealed class ServerDataClient(HttpClient httpClient)
    : CoreApiClient(httpClient, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
{
    public Task<Result<Event>> GetEventAsync(CancellationToken ct = default)
        => GetAsync<Event>("data/event", ct);

    public Task<Result<IReadOnlyList<SalesItem>>> GetSalesItemsAsync(CancellationToken ct = default)
        => GetAsync<IReadOnlyList<SalesItem>>("data/salesitems", ct);
}
