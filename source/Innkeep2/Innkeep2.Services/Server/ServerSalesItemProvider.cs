using Innkeep2.Models.Core;
using Innkeep2.Models.Internal;
using Innkeep2.Requests.Cloud;
using Microsoft.Extensions.Caching.Memory;
using Serilog;

namespace Innkeep2.Services.Server;

public sealed class ServerSalesItemProvider(CloudDataClient client)
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(120);

    private IReadOnlyList<SalesItem>? _lastKnownGood;
    private DateTimeOffset _lastFetchedAt = DateTimeOffset.MinValue;

    public async Task<Result<IReadOnlyList<SalesItem>>> GetCachedItemsAsync(CancellationToken ct = default)
    {
        if (_lastKnownGood is not null && DateTimeOffset.UtcNow - _lastFetchedAt < CacheDuration)
            return Result<IReadOnlyList<SalesItem>>.Success(_lastKnownGood);

        var result = await client.GetSalesItemsAsync(ct);

        if (result.IsSuccess)
        {
            _lastKnownGood = result.Value;
            _lastFetchedAt = DateTimeOffset.UtcNow;
            return result;
        }

        if (_lastKnownGood is not null)
        {
            Log.Warning("Sales item refresh failed, serving stale data from {LastFetchedAt}: {Error}", _lastFetchedAt, result.Error!.Message);
            return Result<IReadOnlyList<SalesItem>>.Success(_lastKnownGood);
        }

        return Result<IReadOnlyList<SalesItem>>.Failure(result.Error!);
    }
}