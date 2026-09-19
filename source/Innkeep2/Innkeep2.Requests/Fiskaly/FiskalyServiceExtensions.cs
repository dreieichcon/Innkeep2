using System.Text.Json;
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
        services.AddTransient<FiskalyAuthHandler>();
        
        services.AddHttpClient<FiskalyAuthClient>(client => client.BaseAddress = new Uri(FiskalyUrls.BaseUrl));
        services.AddSingleton<FiskalyTokenProvider>();
        
        services.AddHttpClient<FiskalyTssClient>(client => client.BaseAddress = new Uri(FiskalyUrls.BaseUrl))
            .AddHttpMessageHandler<FiskalyAuthHandler>();
        
        services.AddHttpClient<FiskalyClientClient>(client => client.BaseAddress = new Uri(FiskalyUrls.BaseUrl))
            .AddHttpMessageHandler<FiskalyAuthHandler>();
        
        services.AddHttpClient<FiskalyTransactionClient>(client => client.BaseAddress = new Uri(FiskalyUrls.BaseUrl))
            .AddHttpMessageHandler<FiskalyAuthHandler>();
    }
}