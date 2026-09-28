using Innkeep2.Models.Sun;
using Innkeep2.Requests.Sun;

namespace Innkeep2.Services.Shared;

public sealed class SunService(SunClient client)
{
    private static readonly SunState Fallback = new()
    {
        Date = DateOnly.FromDateTime(DateTime.UtcNow),
        Sunrise = new TimeOnly(6, 0),
        Sunset = new TimeOnly(18, 0),
        Dusk = new TimeOnly(18, 0)
    };

    private SunState? _state;
    private DateTime _lastUpdate = DateTime.MinValue;

    public async Task<SunState> GetSunStateAsync(double lat, double lng, CancellationToken ct = default)
    {
        if (_state is not null && DateTime.UtcNow - _lastUpdate < TimeSpan.FromHours(24))
            return _state;

        var result = await client.GetAsync(lat, lng, ct);

        if (result.IsSuccess)
        {
            _state = result.Value;
            _lastUpdate = DateTime.UtcNow;
            return _state!;
        }

        return Fallback;
    }

    public bool IsDarkMode(SunState state) => state.IsAfterDark();
}