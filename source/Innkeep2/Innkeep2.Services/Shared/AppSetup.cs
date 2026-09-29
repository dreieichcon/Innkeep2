using Innkeep2.Credentials;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Discord;

namespace Innkeep2.Services.Shared;

public static class AppSetup
{
    public static void SetupLogging(string serviceName, string? webhookUrl = null)
    {
        if (!Directory.Exists("./log"))
            Directory.CreateDirectory("./log");

        var configuration = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.Trace()
            .WriteTo.File("./log/log-.txt", rollingInterval: RollingInterval.Day);
        
        if (webhookUrl is not null)
            configuration.WriteTo.Discord(
                LogEventLevel.Warning,
                config =>
                {
                    config.WebhookUrl = webhookUrl;
                    config.ServiceName = serviceName;
                }
            );
        
        Log.Logger = configuration.CreateLogger();
    }

    public static bool SetupCredentials(string appType, string templateJson, out string credentialsPath)
    {
        credentialsPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "credentials", $"credentials.{appType}.json");
        return CredentialCreator.EnsureExists(credentialsPath, templateJson);
    }
}