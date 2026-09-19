using Innkeep2.Credentials.Models;
using Innkeep2.Requests.Cloud;
using Innkeep2.Requests.Cloud.Auth;
using Innkeep2.Server.Services;
using Innkeep2.Services.Server;

namespace Innkeep2.Server.Extensions;

public static class ServiceCollectionExtensions
{
    public static void RegisterServerServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCloudCredential(configuration);
        services.AddCloudClients();
        
        services.AddCloudCaches();
        
        services.AddSingleton<ServerStartupService>();
    }
    
    private static void AddCloudCredential(this IServiceCollection services, IConfiguration configuration)
    {
        var credential = configuration.GetSection("Cloud").Get<CloudCredential>()
                         ?? throw new InvalidOperationException("Missing 'Cloud' section in configuration.");

        services.AddSingleton(credential);
    }
    
    private static void AddCloudClients(this IServiceCollection services)
    {
        services.AddTransient<CloudAuthHandler>();

        services.AddHttpClient<CloudAuthClient>((sp, client) =>
                client.BaseAddress = new Uri(sp.GetRequiredService<CloudCredential>().CloudUrl))
            .AddHttpMessageHandler<CloudAuthHandler>();

        services.AddHttpClient<CloudDataClient>((sp, client) =>
                client.BaseAddress = new Uri(sp.GetRequiredService<CloudCredential>().CloudUrl))
            .AddHttpMessageHandler<CloudAuthHandler>();

        services.AddHttpClient<CloudTransactionClient>((sp, client) =>
                client.BaseAddress = new Uri(sp.GetRequiredService<CloudCredential>().CloudUrl))
            .AddHttpMessageHandler<CloudAuthHandler>();
    }

    private static void AddCloudCaches(this IServiceCollection services)
    {
        services.AddSingleton<ServerEventProvider>();
        services.AddSingleton<ServerSalesItemProvider>();
    }
}