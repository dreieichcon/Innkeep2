using System.Text.Json;
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
}