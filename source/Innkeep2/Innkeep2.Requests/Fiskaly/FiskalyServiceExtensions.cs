using System.Text.Json;
using Innkeep2.Requests.Fiskaly.Auth;
using Innkeep2.Requests.Serialization.Fiskaly;
using Microsoft.Extensions.DependencyInjection;

namespace Innkeep2.Requests.Fiskaly;

public static class FiskalyServiceCollectionExtensions
{
    public static void AddFiskalySerializerOptions(this IServiceCollection services)
    {
        services.AddKeyedSingleton("fiskaly", (_, _) =>
        {
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
            options.Converters.Add(new FiskalyDecimalConverter());
            return options;
        });
    }

    public static void AddFiskalyClients(this IServiceCollection services)
    {
        services.AddFiskalySerializerOptions();
        services.AddHttpClient<FiskalyAuthClient>(client => client.BaseAddress = new Uri(FiskalyUrls.BaseUrl));
        services.AddSingleton<FiskalyTokenProvider>();
    }
}