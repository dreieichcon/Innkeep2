using Innkeep2.CredentialsDb.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Innkeep2.CredentialsDb;

public static class ServiceProviderExtensions
{
    /// <summary>
    /// Loads the active credentials of the app into their providers.
    /// The callback receives the active Discord webhook, or null if none is active, on load and on every later change.
    /// </summary>
    public static async Task LoadCredentialsAsync<T>(this IServiceProvider services, Action<string?> onDiscordWebhookChanged)
        where T : class, ICredentialDocument
    {
        var discord = services.GetRequiredService<ActiveCredentialProvider<DiscordCredential>>();
        discord.Changed += (_, _) => onDiscordWebhookChanged(discord.HasActive ? discord.GetActive().WebhookUrl : null);

        await discord.ReloadAsync();
        await services.GetRequiredService<ActiveCredentialProvider<T>>().ReloadAsync();
    }
}
