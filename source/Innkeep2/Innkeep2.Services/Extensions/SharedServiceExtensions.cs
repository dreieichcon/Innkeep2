using System.Text.Json;
using Demolite.Http.Extensions;
using Innkeep2.CredentialsDb;
using Innkeep2.CredentialsDb.Models;
using Innkeep2.Requests.Http;
using Innkeep2.Requests.Sun;
using Innkeep2.Services.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace Innkeep2.Services.Extensions;

public static class SharedServiceExtensions
{
    public static void AddSunServices(this IServiceCollection services)
    {
        services.AddKeyedSingleton("sun", (_, _) =>
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        services.AddHttpClient<SunClient>(client =>
            client.BaseAddress = new Uri("https://api.sunrisesunset.io/"));

        services.AddSingleton<SunService>();
        services.AddSingleton<DarkModeService>();
    }

    /// <summary>
    /// Registers a typed HTTP client for the API of another Innkeep application,
    /// using the URL and key of the active API key credential.
    /// Without an active credential the client is still created, and its requests fail without contacting the network.
    /// </summary>
    public static void AddInnkeepApiClient<TClient>(this IServiceCollection services, int timeoutSeconds)
        where TClient : class
        => services.AddHttpClient<TClient>((sp, client) =>
            {
                client.BaseAddress = GetActiveApiKey(sp) is { } apiKey
                    ? new Uri(apiKey.Url.TrimEnd('/') + "/")
                    : MissingCredentialHandler.PlaceholderAddress;

                client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            })
            .AddMissingCredentialHandler(sp => GetActiveApiKey(sp) is not null)
            .AddHeader("X-Api-Key", sp => GetActiveApiKey(sp)?.ApiKey ?? string.Empty);

    private static InnkeepApiKey? GetActiveApiKey(IServiceProvider sp)
        => sp.GetRequiredService<ActiveCredentialProvider<InnkeepApiKey>>().Active;
}