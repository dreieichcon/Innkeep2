using Innkeep2.Requests.Server;
using Innkeep2.Services.Client;
using Innkeep2.Services.Shared;
using Serilog;

namespace Innkeep2.Client.Services;

public class ClientStartupService(ServerAuthClient authClient, ClientEventProvider eventProvider, ClientSalesItemProvider salesItemProvider, DarkModeService darkModeService)
{
    public async Task<bool> RunAsync(CancellationToken ct = default)
    {
        _ = Task.Run(() => darkModeService.StartPollingAsync(ct), ct);

        Log.Debug("Authenticating against Innkeep2.Server");
        var authStatus = await authClient.CheckAsync(ct);

        if (!authStatus.IsSuccess)
        {
            var error = authStatus.Error!;

            if (error.Exception is not null)
                Log.Warning(error.Exception, "Could not authenticate with Server yet ({Code}): {Message}", error.Code, error.Message);
            else
                Log.Warning("Could not authenticate with Server yet ({Code}): {Message}", error.Code, error.Message);
        }
        else
        {
            Log.Debug("Innkeep2.Server Authentication Successful");
        }

        Log.Debug("Fetching registered Event");
        var eventResult = await eventProvider.GetCachedEventAsync(ct);

        if (!eventResult.IsSuccess)
            Log.Warning("Could not fetch event yet ({Code}): {Message}", eventResult.Error!.Code, eventResult.Error!.Message);
        else
            Log.Debug("Registered Event: {Event}", eventResult.Value!.Name);

        Log.Debug("Fetching registered SalesItems");
        var salesItemsResult = await salesItemProvider.GetCachedItemsAsync(ct);
        _ = salesItemProvider.StartPollingAsync(ct);

        if (!salesItemsResult.IsSuccess)
            Log.Warning("Could not fetch sales items yet ({Code}): {Message}", salesItemsResult.Error!.Code, salesItemsResult.Error!.Message);
        else
            Log.Debug("Fetched {Count} Items", salesItemsResult.Value!.Count);

        return true;
    }
}