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

    public async Task<Result<Event>> GetCachedEventAsync(CancellationToken ct = default)
    {
        if (_lastKnownGood is not null && DateTimeOffset.UtcNow - _lastFetchedAt < CacheDuration)
            return Result<Event>.Success(_lastKnownGood);

        var result = await client.GetEventAsync(ct);

        if (result.IsSuccess)
        {
            _lastKnownGood = result.Value;
            _lastFetchedAt = DateTimeOffset.UtcNow;
            return result;
        }

        if (_lastKnownGood is not null)
        {
            Log.Warning("Event refresh failed, serving stale data from {LastFetchedAt}: {Error}", _lastFetchedAt, result.Error!.Message);
            return Result<Event>.Success(_lastKnownGood);
        }

        return Result<Event>.Failure(result.Error!);
    }
}