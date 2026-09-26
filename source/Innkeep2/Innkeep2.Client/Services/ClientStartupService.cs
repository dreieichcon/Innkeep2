using Innkeep2.Requests.Server;
using Innkeep2.Services.Client;
using Serilog;

namespace Innkeep2.Client.Services;

public class ClientStartupService(ServerAuthClient authClient, ClientEventProvider eventProvider, ClientSalesItemProvider salesItemProvider)
{
    public async Task<bool> RunAsync(CancellationToken ct = default)
    {
        Log.Debug("Authenticating against Innkeep2.Server");
        var authStatus = await authClient.CheckAsync(ct);

        if (!authStatus.IsSuccess)
        {
            var error = authStatus.Error!;

            if (error.Exception is not null)
                Log.Error(error.Exception, "Startup failed: could not authenticate with Server ({Code}): {Message}",
                    error.Code, error.Message);
            else
                Log.Error("Startup failed: could not authenticate with Server ({Code}): {Message}", error.Code,
                    error.Message);

            return false;
        }

        Log.Debug("Innkeep2.Cloud Authentication Successful");

        Log.Debug("Fetching registered Event");
        var eventResult = await eventProvider.GetCachedEventAsync(ct);

        if (!eventResult.IsSuccess)
            Log.Error("Startup failed: could not fetch event ({Code}): {Message}", eventResult.Error!.Code, eventResult.Error!.Message);

        Log.Debug("Registered Event: {Event}", eventResult.Value!.Name);
        
        Log.Debug("Fetching registered SalesItems");
        var salesItemsResult = await salesItemProvider.GetCachedItemsAsync(ct);
        _ = salesItemProvider.StartPollingAsync(ct);
        
        if (!salesItemsResult.IsSuccess)
            Log.Error("Startup failed: could not fetch sales items ({Code}): {Message}", salesItemsResult.Error!.Code, salesItemsResult.Error!.Message);

        Log.Debug("Fetched {Count} Items", salesItemsResult.Value!.Count);
        
        return true;
    }
}