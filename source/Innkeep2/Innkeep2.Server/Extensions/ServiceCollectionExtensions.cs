using Innkeep2.CredentialsDb;
using Innkeep2.Print.Printer;
using Innkeep2.Print.Storage;
using Innkeep2.Requests.Cloud;
using Innkeep2.Server.Queue;
using Innkeep2.Server.Security;
using Innkeep2.Server.Services;
using Innkeep2.Services.Extensions;
using Innkeep2.Services.Server;

namespace Innkeep2.Server.Extensions;

public static class ServiceCollectionExtensions
{
    public static void RegisterServerServices(this IServiceCollection services)
    {
        services.AddApiKeyCredentialsDb();
        services.AddCloudClients();
        services.RegisterPrinterServices();
        
        services.AddCloudCaches();
        
        services.RegisterServerDatabase();
        services.AddSingleton<ServerTransactionService>();
        services.AddSingleton<RequestRetryService>();
        services.AddHttpClient<ImageFetchService>();
        services.AddSunServices();
        
        services.AddSingleton<ServerStartupService>();
    }
    
    public static void AddCloudClients(this IServiceCollection services)
    {
        services.AddInnkeepApiClient<CloudAuthClient>(timeoutSeconds: 5);
        services.AddInnkeepApiClient<CloudDataClient>(timeoutSeconds: 5);
        services.AddInnkeepApiClient<CloudTransactionClient>(timeoutSeconds: 10);
        services.AddInnkeepApiClient<CloudPaymentClient>(timeoutSeconds: 15);
        services.AddTransient<ICloudPaymentClient>(sp => sp.GetRequiredService<CloudPaymentClient>());
        services.AddSingleton<ServerPaymentService>();
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
