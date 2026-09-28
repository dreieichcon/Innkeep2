namespace Innkeep2.Services.Shared;

public sealed class DarkModeService(SunService sunService)
{
    private const double Latitude = 50.02485985471239;
    private const double Longitude = 8.66270997993345;

    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);

    public bool IsDarkMode { get; private set; }

    public event EventHandler? Changed;

    public async Task StartPollingAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(CheckInterval);

        await RefreshAsync(ct);

        while (await timer.WaitForNextTickAsync(ct))
            await RefreshAsync(ct);
    }

    private async Task RefreshAsync(CancellationToken ct)
    {
        var state = await sunService.GetSunStateAsync(Latitude, Longitude, ct);
        var isDark = state.IsAfterDark();

        if (isDark != IsDarkMode)
        {
            IsDarkMode = isDark;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}