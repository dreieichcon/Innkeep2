using Demolite.Core.Models;
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

    public DateTimeOffset? LastFetchedAt => _lastFetchedAt == DateTimeOffset.MinValue ? null : _lastFetchedAt;

    public bool? CloudAvailable { get; private set; }

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
            StoreFetchedItems(result.Value!);
            return result;
        }

        CloudAvailable = false;

        return Result<IReadOnlyList<SalesItem>>.Failure(result.Error!);
    }

    /// <summary>
    /// Reloads items and stock from Pretix through Cloud. Returns the cached items when Cloud is unreachable.
    /// </summary>
    public Task<SalesItemsResult> ForceRefreshAsync(CancellationToken ct = default)
        => RefreshFromCloudAsync(client.RefreshSalesItemsAsync, ct);

    /// <summary>
    /// Reloads only the stock from Pretix through Cloud. Returns the cached items when Cloud is unreachable.
    /// </summary>
    public Task<SalesItemsResult> RefreshStockAsync(CancellationToken ct = default)
        => RefreshFromCloudAsync(client.RefreshSalesItemStockAsync, ct);

    private async Task<SalesItemsResult> RefreshFromCloudAsync(
        Func<CancellationToken, Task<Result<SalesItemsResult>>> refresh,
        CancellationToken ct
    )
    {
        var result = await refresh(ct);

        if (result.IsSuccess)
        {
            StoreFetchedItems(result.Value!.Items);
            return new SalesItemsResult(result.Value.Items, true);
        }

        CloudAvailable = false;

        Log.Warning("Sales item refresh through Cloud failed, serving cached data from {LastFetchedAt}: {Error}", _lastFetchedAt, result.Error!.Message);

        return new SalesItemsResult(_lastKnownGood ?? [], false);
    }

    private void StoreFetchedItems(IReadOnlyList<SalesItem> items)
    {
        _lastKnownGood = items;
        _lastFetchedAt = DateTimeOffset.UtcNow;
        _nextRefreshAt = _lastFetchedAt + CacheDuration;
        CloudAvailable = true;
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
                    StoreFetchedItems(result.Value!);
                }
                else
                {
                    Log.Warning("Sales item refresh failed, serving stale data from {LastFetchedAt}: {Error}", _lastFetchedAt, result.Error!.Message);
                    _nextRefreshAt = DateTimeOffset.UtcNow + CacheDuration;
                    CloudAvailable = false;
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
