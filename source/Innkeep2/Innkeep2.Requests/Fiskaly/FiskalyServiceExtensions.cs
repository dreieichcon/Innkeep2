using System.Text.Json;
using Demolite.Http.Extensions;
using Innkeep2.Models.Serialization.Fiskaly;
using Innkeep2.Requests.Fiskaly.Auth;
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
        
        services.AddApiClient<FiskalyAuthClient>(FiskalyUrls.BaseUrl);
        services.AddSingleton<FiskalyTokenProvider>();
        
        services.AddApiClient<FiskalyTssClient>(FiskalyUrls.BaseUrl).AddAuth<FiskalyTokenProvider>();
        services.AddApiClient<FiskalyClientClient>(FiskalyUrls.BaseUrl).AddAuth<FiskalyTokenProvider>();
        services.AddApiClient<FiskalyTransactionClient>(FiskalyUrls.BaseUrl).AddAuth<FiskalyTokenProvider>();
    }
}
