using Innkeep2.Services.Cloud;
using Innkeep2.Services.Shared;
using Serilog;

namespace Innkeep2.Cloud.Services;

public class CloudStartupService(IActiveConfigurationService activeConfiguration, DarkModeService darkModeService)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        _ = Task.Run(() => darkModeService.StartPollingAsync(ct), ct);

        Log.Debug("Loading active configuration");

        try
        {
            var result = await activeConfiguration.RefreshAsync(ct);

            if (!result.IsSuccess)
            {
                Log.Warning("Could not load active configuration ({Code}): {Message}", result.Error!.Code, result.Error.Message);
                return;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not load active configuration");
            return;
        }

        LogConfiguration();
    }

    private void LogConfiguration()
    {
        Log.Debug("Organizer: {Organizer}", activeConfiguration.Organizer?.Slug ?? "none");
        Log.Debug("Event: {Event}", activeConfiguration.Event?.Name ?? "none");
        Log.Debug("TSS: {Tss}", activeConfiguration.Tss?.Id.ToString() ?? "none");
        Log.Debug("Fiskaly Client: {Client}", activeConfiguration.Client?.Id.ToString() ?? "none");
        Log.Debug("Order Database: {Path}", activeConfiguration.OrderDatabasePath ?? "none");

        if (activeConfiguration.Event is null || activeConfiguration.Tss is null ||
            activeConfiguration.Client is null || activeConfiguration.OrderDatabasePath is null)
            Log.Warning("Active configuration is incomplete, transactions will be rejected until it is set up");
    }
}
