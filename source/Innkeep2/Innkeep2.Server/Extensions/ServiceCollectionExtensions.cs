using Innkeep2.Credentials.Models;
using Innkeep2.Print.Printer;
using Innkeep2.Print.Storage;
using Innkeep2.Requests.Cloud;
using Innkeep2.Requests.Cloud.Auth;
using Innkeep2.Server.Queue;
using Innkeep2.Server.Security;
using Innkeep2.Server.Services;
using Innkeep2.Services.Extensions;
using Innkeep2.Services.Server;

namespace Innkeep2.Server.Extensions;

public static class ServiceCollectionExtensions
{
    public static void RegisterServerServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCloudCredential(configuration);
        services.AddCloudClients();
        services.RegisterPrinterServices();
        
        services.AddCloudCaches();
        
        services.RegisterServerDatabase();
        services.AddSingleton<ServerTransactionService>();
        services.AddSingleton<RequestRetryService>();
        services.AddSunServices();
        
        services.AddSingleton<ServerStartupService>();
    }
    
    public static void AddCloudCredential(this IServiceCollection services, IConfiguration configuration)
    {
        var credential = configuration.GetSection("Cloud").Get<CloudCredential>()
                         ?? throw new InvalidOperationException("Missing 'Cloud' section in configuration.");

        services.AddSingleton(credential);
    }
    
    public static void AddCloudClients(this IServiceCollection services)
    {
        services.AddTransient<CloudAuthHandler>();

        services.AddHttpClient<CloudAuthClient>((sp, client) =>
            {
                client.BaseAddress = new Uri(sp.GetRequiredService<CloudCredential>().CloudUrl);
                client.Timeout = TimeSpan.FromSeconds(5);
            })
            .AddHttpMessageHandler<CloudAuthHandler>();

        services.AddHttpClient<CloudDataClient>((sp, client) =>
            {
                client.BaseAddress = new Uri(sp.GetRequiredService<CloudCredential>().CloudUrl);
                client.Timeout = TimeSpan.FromSeconds(5);
            })
            .AddHttpMessageHandler<CloudAuthHandler>();

        services.AddHttpClient<CloudTransactionClient>((sp, client) =>
            {
                client.BaseAddress = new Uri(sp.GetRequiredService<CloudCredential>().CloudUrl);
                client.Timeout = TimeSpan.FromSeconds(10);
            })
            .AddHttpMessageHandler<CloudAuthHandler>();
    }

    private static void AddCloudCaches(this IServiceCollection services)
    {
        services.AddSingleton<ServerEventProvider>();
        services.AddSingleton<ServerSalesItemProvider>();
    }
    
    private static void RegisterServerDatabase(this IServiceCollection services)
    {
        if (!Directory.Exists("./db"))
            Directory.CreateDirectory("./db");

        const string databasePath = "./db/server.db";

        services.AddSingleton(new RequestQueueRepository(databasePath));
        services.AddSingleton(new ApiKeyRepository(databasePath));
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