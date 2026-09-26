using Innkeep2.Credentials;
using Serilog;

namespace Innkeep2.Services.Shared;

public static class AppSetup
{
    public static void SetupLogging()
    {
        if (!Directory.Exists("./log"))
            Directory.CreateDirectory("./log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.Trace()
            .WriteTo.File("./log/log-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }

    public static bool SetupCredentials(string appType, string templateJson, out string credentialsPath)
    {
        credentialsPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "credentials", $"credentials.{appType}.json");
        return CredentialCreator.EnsureExists(credentialsPath, templateJson);
    }
}