using Innkeep2.Client.Services;
using Innkeep2.Credentials.Models;
using Innkeep2.Requests.Server;
using Innkeep2.Requests.Server.Auth;
using Innkeep2.Services.Client;

namespace Innkeep2.Client.Extensions;

public static class ServerServiceCollectionExtensions
{
    public static void RegisterClientServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddServerCredential(configuration);
        services.AddServerClients();
        services.AddSingleton<ClientSalesItemProvider>();
        
        services.AddSingleton<ClientCartService>();
        services.AddSingleton<ClientStartupService>();

    }
    
    private static void AddServerCredential(this IServiceCollection services, IConfiguration configuration)
    {
        var credential = configuration.GetSection("Server").Get<ServerCredential>()
                         ?? throw new InvalidOperationException("Missing 'Server' section in configuration.");

        services.AddSingleton(credential);
    }
    
    private static void AddServerClients(this IServiceCollection services)
    {
        services.AddTransient<ServerAuthHandler>();

        services.AddHttpClient<ServerAuthClient>((sp, client) =>
                client.BaseAddress = new Uri(sp.GetRequiredService<ServerCredential>().ServerUrl))
            .AddHttpMessageHandler<ServerAuthHandler>();

        services.AddHttpClient<ServerDataClient>((sp, client) =>
                client.BaseAddress = new Uri(sp.GetRequiredService<ServerCredential>().ServerUrl))
            .AddHttpMessageHandler<ServerAuthHandler>();

        services.AddHttpClient<ServerTransactionClient>((sp, client) =>
                client.BaseAddress = new Uri(sp.GetRequiredService<ServerCredential>().ServerUrl))
            .AddHttpMessageHandler<ServerAuthHandler>();
    }
}