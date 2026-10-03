using Innkeep2.Models.Core;
using Innkeep2.Models.Internal;
using Innkeep2.Requests.Cloud;
using Serilog;

namespace Innkeep2.Services.Server;

public sealed class ServerSalesItemProvider(CloudDataClient client)
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(120);

    private IReadOnlyList<SalesItem>? _lastKnownGood;
    private DateTimeOffset _lastFetchedAt = DateTimeOffset.MinValue;
    private DateTimeOffset _nextRefreshAt = DateTimeOffset.MinValue;
    private int _refreshing;

    public async Task<Result<IReadOnlyList<SalesItem>>> GetCachedItemsAsync(CancellationToken ct = default)
    {
        if (_lastKnownGood is { } cached)
        {
            if (DateTimeOffset.UtcNow >= _nextRefreshAt)
                StartBackgroundRefresh();

            return Result<IReadOnlyList<SalesItem>>.Success(cached);
        }

        var result = await client.GetSalesItemsAsync(ct);

        if (result.IsSuccess)
        {
            _lastKnownGood = result.Value;
            _lastFetchedAt = DateTimeOffset.UtcNow;
            _nextRefreshAt = _lastFetchedAt + CacheDuration;
            return result;
        }

        return Result<IReadOnlyList<SalesItem>>.Failure(result.Error!);
    }

    public async Task<Result<IReadOnlyList<SalesItem>>> ForceRefreshAsync(CancellationToken ct = default)
    {
        var result = await client.GetSalesItemsAsync(ct);

        if (result.IsSuccess)
        {
            _lastKnownGood = result.Value;
            _lastFetchedAt = DateTimeOffset.UtcNow;
            _nextRefreshAt = _lastFetchedAt + CacheDuration;
        }

        return result;
    }

    private void StartBackgroundRefresh()
    {
        if (Interlocked.CompareExchange(ref _refreshing, 1, 0) != 0)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                var result = await client.GetSalesItemsAsync(CancellationToken.None);

                if (result.IsSuccess)
                {
                    _lastKnownGood = result.Value;
                    _lastFetchedAt = DateTimeOffset.UtcNow;
                    _nextRefreshAt = _lastFetchedAt + CacheDuration;
                }
                else
                {
                    Log.Warning("Sales item refresh failed, serving stale data from {LastFetchedAt}: {Error}", _lastFetchedAt, result.Error!.Message);
                    _nextRefreshAt = DateTimeOffset.UtcNow + CacheDuration;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Sales item refresh failed unexpectedly");
            }
            finally
            {
                Interlocked.Exchange(ref _refreshing, 0);
            }
        });
    }
}
