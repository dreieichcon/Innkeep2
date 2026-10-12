using Serilog;
using Serilog.Events;
using Serilog.Sinks.Discord;

namespace Innkeep2.Services.Shared;

public static class AppSetup
{
    private static readonly SwitchableSink DiscordSink = new();
    private static string _serviceName = string.Empty;

    /// <summary>
    /// Configures the global logger. Warnings are forwarded to Discord once a webhook is configured.
    /// </summary>
    public static void SetupLogging(string serviceName, string? webhookUrl = null)
    {
        if (!Directory.Exists("./log"))
            Directory.CreateDirectory("./log");

        _serviceName = serviceName;

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.Trace()
            .WriteTo.File("./log/log-.txt", rollingInterval: RollingInterval.Day)
            .WriteTo.Sink(DiscordSink, LogEventLevel.Warning)
            .CreateLogger();

        ConfigureDiscord(webhookUrl);
    }

    /// <summary>
    /// Sets or removes the Discord webhook of the running logger. An empty value disables Discord logging.
    /// </summary>
    public static void ConfigureDiscord(string? webhookUrl)
        => DiscordSink.Replace(string.IsNullOrWhiteSpace(webhookUrl) ? null : CreateDiscordSink(webhookUrl));

    private static BackgroundSink CreateDiscordSink(string webhookUrl)
    {
        var discordLogger = new LoggerConfiguration()
            .WriteTo.Discord(
                LogEventLevel.Warning,
                config =>
                {
                    config.WebhookUrl = webhookUrl;
                    config.ServiceName = _serviceName;
                }
            )
            .CreateLogger();

        return new BackgroundSink(discordLogger);
    }
}
