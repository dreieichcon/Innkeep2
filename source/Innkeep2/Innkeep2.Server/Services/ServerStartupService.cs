using Innkeep2.Requests.Cloud;
using Innkeep2.Services.Server;
using Serilog;

namespace Innkeep2.Server.Services;

public class ServerStartupService(
    CloudAuthClient authClient,
    ServerEventProvider eventProvider,
    ServerSalesItemProvider salesItemProvider)
{
    public async Task<bool> RunAsync(CancellationToken ct = default)
    {
        Log.Debug("Authenticating against Innkeep2.Cloud");
        var authStatus = await authClient.CheckAsync(ct);

        if (!authStatus.IsSuccess)
        {
            var error = authStatus.Error!;

            if (error.Exception is not null)
                Log.Error(error.Exception, "Startup failed: could not authenticate with Cloud ({Code}): {Message}",
                    error.Code, error.Message);
            else
                Log.Error("Startup failed: could not authenticate with Cloud ({Code}): {Message}", error.Code,
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
        
        if (!salesItemsResult.IsSuccess)
            Log.Error("Startup failed: could not fetch sales items ({Code}): {Message}", salesItemsResult.Error!.Code, salesItemsResult.Error!.Message);

        Log.Debug("Fetched {Count} Items", salesItemsResult.Value!.Count);
        
        return true;
    }
}