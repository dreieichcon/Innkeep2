using Innkeep2.Models.Core;
using Innkeep2.Models.Internal;
using Innkeep2.Requests.Server;

namespace Innkeep2.Services.Client;

public sealed class ClientSalesItemProvider(ServerDataClient client)
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(60);

    private IReadOnlyList<SalesItem>? _cached;
    private DateTimeOffset _fetchedAt = DateTimeOffset.MinValue;

    public async Task<Result<IReadOnlyList<SalesItem>>> GetCachedItemsAsync(CancellationToken ct = default)
    {
        if (_cached is not null && DateTimeOffset.UtcNow - _fetchedAt < CacheDuration)
            return Result<IReadOnlyList<SalesItem>>.Success(_cached);

        var result = await client.GetSalesItemsAsync(ct);

        if (!result.IsSuccess)
            return result;

        _cached = result.Value;
        _fetchedAt = DateTimeOffset.UtcNow;

        return result;
    }
}