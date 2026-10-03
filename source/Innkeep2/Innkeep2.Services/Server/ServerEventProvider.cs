using Innkeep2.Models.Core;
using Innkeep2.Models.Internal;
using Innkeep2.Requests.Cloud;
using Serilog;

namespace Innkeep2.Services.Server;

public sealed class ServerEventProvider(CloudDataClient client)
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(120);

    private Event? _lastKnownGood;
    private DateTimeOffset _lastFetchedAt = DateTimeOffset.MinValue;
    private DateTimeOffset _nextRefreshAt = DateTimeOffset.MinValue;
    private int _refreshing;

    /// <summary>
    /// The last event fetched from the cloud, without ever contacting it. Null until the first successful fetch.
    /// </summary>
    public Event? LastKnownEvent => _lastKnownGood;

    public async Task<Result<Event>> GetCachedEventAsync(CancellationToken ct = default)
    {
        if (_lastKnownGood is { } cached)
        {
            if (DateTimeOffset.UtcNow >= _nextRefreshAt)
                StartBackgroundRefresh();

            return Result<Event>.Success(cached);
        }

        var result = await client.GetEventAsync(ct);

        if (result.IsSuccess)
        {
            _lastKnownGood = result.Value;
            _lastFetchedAt = DateTimeOffset.UtcNow;
            _nextRefreshAt = _lastFetchedAt + CacheDuration;
            return result;
        }

        return Result<Event>.Failure(result.Error!);
    }

    private void StartBackgroundRefresh()
    {
        if (Interlocked.CompareExchange(ref _refreshing, 1, 0) != 0)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                var result = await client.GetEventAsync(CancellationToken.None);

                if (result.IsSuccess)
                {
                    _lastKnownGood = result.Value;
                    _lastFetchedAt = DateTimeOffset.UtcNow;
                    _nextRefreshAt = _lastFetchedAt + CacheDuration;
                }
                else
                {
                    Log.Warning("Event refresh failed, serving stale data from {LastFetchedAt}: {Error}", _lastFetchedAt, result.Error!.Message);
                    _nextRefreshAt = DateTimeOffset.UtcNow + CacheDuration;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Event refresh failed unexpectedly");
            }
            finally
            {
                Interlocked.Exchange(ref _refreshing, 0);
            }
        });
    }
}
