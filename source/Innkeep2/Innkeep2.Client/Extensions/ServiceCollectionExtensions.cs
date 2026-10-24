using Innkeep2.Client.Services;
using Innkeep2.CredentialsDb;
using Innkeep2.Print.Printer;
using Innkeep2.Print.Storage;
using Innkeep2.Requests.Server;
using Innkeep2.Services.Client;
using Innkeep2.Services.Extensions;
using Innkeep2.Services.Shared;

namespace Innkeep2.Client.Extensions;

public static class ServiceCollectionExtensions
{
    public static void RegisterClientServices(this IServiceCollection services)
    {
        services.AddSingleton<LoadingService>();
        services.AddApiKeyCredentialsDb();
        services.AddServerClients();
        services.RegisterPrinterServices();
        services.AddSingleton<ClientEventProvider>();
        services.AddSingleton<ClientSalesItemProvider>();
        
        services.AddSingleton<ClientCartService>();
        services.AddSingleton<ClientCartAdditionService>();
        services.AddSingleton<CustomerDisplayService>();
        services.AddSingleton<ClientTransactionService>();
        
        services.AddSunServices();
        
        services.AddSingleton<ClientStartupService>();
    }
    
    private static void AddServerClients(this IServiceCollection services)
    {
        services.AddInnkeepApiClient<ServerAuthClient>(timeoutSeconds: 5);
        services.AddInnkeepApiClient<ServerDataClient>(timeoutSeconds: 10);
        services.AddInnkeepApiClient<ServerTransactionClient>(timeoutSeconds: 30);
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
