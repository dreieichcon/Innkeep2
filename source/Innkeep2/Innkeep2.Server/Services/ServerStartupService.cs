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

        Log.Debug("Fetching registered Event");
        var e = await eventProvider.GetCachedEventAsync(ct);
        
        Log.Debug("Fetching registered SalesItems");
        var s = await salesItemProvider.GetCachedItemsAsync(ct);
        return true;
    }
}