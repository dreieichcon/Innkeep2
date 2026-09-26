using Innkeep2.Models.Core;
using Innkeep2.Models.Internal;
using Innkeep2.Requests.Server;

namespace Innkeep2.Services.Client;

public sealed class ClientEventProvider(ServerDataClient client)
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(60);

    private Event? _cached;
    private DateTimeOffset _fetchedAt = DateTimeOffset.MinValue;

    public async Task<Result<Event>> GetCachedEventAsync(CancellationToken ct = default)
    {
        if (_cached is not null && DateTimeOffset.UtcNow - _fetchedAt < CacheDuration)
            return Result<Event>.Success(_cached);

        var result = await client.GetEventAsync(ct);

        if (!result.IsSuccess)
            return result;

        _cached = result.Value;
        _fetchedAt = DateTimeOffset.UtcNow;

        return result;
    }
}