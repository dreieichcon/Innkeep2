using Innkeep2.Client.Services;
using Innkeep2.Credentials.Models;
using Innkeep2.Print.Printer;
using Innkeep2.Print.Storage;
using Innkeep2.Requests.Server;
using Innkeep2.Requests.Server.Auth;
using Innkeep2.Services.Client;
using Innkeep2.Services.Extensions;
using Innkeep2.Services.Shared;

namespace Innkeep2.Client.Extensions;

public static class ServiceCollectionExtensions
{
    public static void RegisterClientServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<LoadingService>();
        services.AddServerCredential(configuration);
        services.AddServerClients();
        services.RegisterPrinterServices();
        services.AddSingleton<ClientEventProvider>();
        services.AddSingleton<ClientSalesItemProvider>();
        
        services.AddSingleton<ClientCartService>();
        services.AddSingleton<ClientTransactionService>();
        
        services.AddSunServices();
        
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
            {
                client.BaseAddress = new Uri(sp.GetRequiredService<ServerCredential>().ServerUrl.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(5);
            })
            .AddHttpMessageHandler<ServerAuthHandler>();

        services.AddHttpClient<ServerDataClient>((sp, client) =>
            {
                client.BaseAddress = new Uri(sp.GetRequiredService<ServerCredential>().ServerUrl.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(10);
            })
            .AddHttpMessageHandler<ServerAuthHandler>();

        services.AddHttpClient<ServerTransactionClient>((sp, client) =>
            {
                client.BaseAddress = new Uri(sp.GetRequiredService<ServerCredential>().ServerUrl.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<ServerAuthHandler>();
    }

    private static void RegisterPrinterServices(this IServiceCollection services)
    {
        if (!Directory.Exists("./db"))
            Directory.CreateDirectory("./db");

        const string databasePath = "./db/printerconfig.db";

        services.AddSingleton(new PrinterSettingsRepository(databasePath));
        services.AddSingleton<NetworkPrinterService>();
    }
}