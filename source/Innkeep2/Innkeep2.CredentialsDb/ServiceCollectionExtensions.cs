using Innkeep2.CredentialsDb.Interfaces;
using Innkeep2.CredentialsDb.Models;
using Innkeep2.CredentialsDb.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Innkeep2.CredentialsDb;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the credentials database for Cloud: credential pairs and Discord credentials.
    /// </summary>
    public static void AddCloudCredentialsDb(this IServiceCollection services)
    {
        var databasePath = CredentialsDbLocation.EnsureCreated();

        services.AddActiveCredential(new CredentialPairRepository(databasePath));
        services.AddActiveCredential(new DiscordCredentialRepository(databasePath));
    }

    /// <summary>
    /// Registers the credentials database for Client and Server: API keys and Discord credentials.
    /// </summary>
    public static void AddApiKeyCredentialsDb(this IServiceCollection services)
    {
        var databasePath = CredentialsDbLocation.EnsureCreated();

        services.AddActiveCredential(new InnkeepApiKeyRepository(databasePath));
        services.AddActiveCredential(new DiscordCredentialRepository(databasePath));
    }

    private static void AddActiveCredential<T>(this IServiceCollection services, ICredentialRepository<T> repository)
        where T : class, ICredentialDocument
    {
        services.AddSingleton(repository);
        services.AddSingleton<ActiveCredentialProvider<T>>();
    }
}
