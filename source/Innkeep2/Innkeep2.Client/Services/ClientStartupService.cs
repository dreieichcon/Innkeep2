using Innkeep2.Requests.Server;
using Serilog;

namespace Innkeep2.Client.Services;

public class ClientStartupService(ServerAuthClient authClient)
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

        return true;
    }
}