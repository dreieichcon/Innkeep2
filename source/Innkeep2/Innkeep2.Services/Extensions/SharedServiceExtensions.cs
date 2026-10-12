using System.Text.Json;
using Demolite.Http.Extensions;
using Innkeep2.CredentialsDb;
using Innkeep2.CredentialsDb.Models;
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
    /// </summary>
    public static void AddInnkeepApiClient<TClient>(this IServiceCollection services, int timeoutSeconds)
        where TClient : class
        => services.AddHttpClient<TClient>((sp, client) =>
            {
                client.BaseAddress = new Uri(GetActiveApiKey(sp).Url.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            })
            .AddHeader("X-Api-Key", sp => GetActiveApiKey(sp).ApiKey);

    private static InnkeepApiKey GetActiveApiKey(IServiceProvider sp)
        => sp.GetRequiredService<ActiveCredentialProvider<InnkeepApiKey>>().GetActive();
}