using System.Text.Json;
using Innkeep2.Models.Core;
using Innkeep2.Models.Internal;
using Innkeep2.Requests.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Innkeep2.Requests.Cloud;

public sealed class CloudDataClient(HttpClient httpClient)
    : CoreApiClient(httpClient, JsonSerializerOptions.Default)
{
    public Task<Result<Event>> GetEventAsync(CancellationToken ct = default)
        => GetAsync<Event>("data/event", ct);

    public Task<Result<IReadOnlyList<SalesItem>>> GetSalesItemsAsync(CancellationToken ct = default)
        => GetAsync<IReadOnlyList<SalesItem>>("data/salesitems", ct);
}