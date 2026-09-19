using Innkeep2.Credentials.Models;
using Innkeep2.Requests.Cloud;
using Innkeep2.Requests.Cloud.Auth;

namespace Innkeep2.Server.Extensions;

public static class ServiceCollectionExtensions
{
    public static void RegisterServerServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCloudCredential(configuration);
        services.AddCloudClients();
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

        services.AddHttpClient<CloudAuthClient>()
            .AddHttpMessageHandler<CloudAuthHandler>();

        services.AddHttpClient<CloudDataClient>()
            .AddHttpMessageHandler<CloudAuthHandler>();

        services.AddHttpClient<CloudTransactionClient>()
            .AddHttpMessageHandler<CloudAuthHandler>();
    }
}