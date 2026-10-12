using Innkeep2.CredentialsDb;
using Innkeep2.CredentialsDb.Models;
using Innkeep2.Requests.Server;
using Innkeep2.Services.Client;
using Innkeep2.Services.Shared;
using Serilog;

namespace Innkeep2.Client.Services;

public class ClientStartupService(
    ServerAuthClient authClient,
    ClientEventProvider eventProvider,
    ClientSalesItemProvider salesItemProvider,
    DarkModeService darkModeService,
    ActiveCredentialProvider<InnkeepApiKey> credentials)
{
    /// <summary>
    /// Runs the startup checks against Server. Failures are logged and never stop the application.
    /// </summary>
    public async Task RunAsync(CancellationToken ct = default)
    {
        _ = Task.Run(() => darkModeService.StartPollingAsync(ct), ct);

        if (!credentials.HasActive)
        {
            Log.Warning("No active Server API key configured. Set it up at /Config/Credentials and restart.");
            return;
        }

        await AuthenticateAsync(ct);
        await LoadEventAsync(ct);
        await LoadSalesItemsAsync(ct);
    }

    private async Task AuthenticateAsync(CancellationToken ct)
    {
        Log.Debug("Authenticating against Innkeep2.Server");
        var authStatus = await authClient.CheckAsync(ct);

        if (authStatus.IsSuccess)
        {
            Log.Debug("Innkeep2.Server Authentication Successful");
            return;
        }

        var error = authStatus.Error!;

        if (error.Exception is not null)
            Log.Warning(error.Exception, "Could not authenticate with Server yet ({Code}): {Message}", error.Code,
                error.Message);
        else
            Log.Warning("Could not authenticate with Server yet ({Code}): {Message}", error.Code, error.Message);
    }

    private async Task LoadEventAsync(CancellationToken ct)
    {
        Log.Debug("Fetching registered Event");
        var eventResult = await eventProvider.GetCachedEventAsync(ct);

        if (!eventResult.IsSuccess)
            Log.Warning("Could not fetch event yet ({Code}): {Message}", eventResult.Error!.Code,
                eventResult.Error!.Message);
        else
            Log.Debug("Registered Event: {Event}", eventResult.Value!.Name);
    }

    private async Task LoadSalesItemsAsync(CancellationToken ct)
    {
        Log.Debug("Fetching registered SalesItems");
        var salesItemsResult = await salesItemProvider.GetCachedItemsAsync(ct);
        _ = salesItemProvider.StartPollingAsync(ct);

        if (!salesItemsResult.IsSuccess)
            Log.Warning("Could not fetch sales items yet ({Code}): {Message}", salesItemsResult.Error!.Code,
                salesItemsResult.Error!.Message);
        else
            Log.Debug("Fetched {Count} Items", salesItemsResult.Value!.Count);
    }
}
