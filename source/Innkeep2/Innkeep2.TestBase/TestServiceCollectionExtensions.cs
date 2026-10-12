using Innkeep2.CredentialsDb;
using Innkeep2.CredentialsDb.Models;
using Innkeep2.CredentialsDb.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Innkeep2.TestBase;

public static class TestServiceCollectionExtensions
{
    /// <summary>
    /// Registers the active credential pair loaded from a temporary copy of the Cloud dev database.
    /// Marks the test as inconclusive if the database or an active credential pair is missing.
    /// </summary>
    public static void AddDevCredentialPair(this IServiceCollection services)
    {
        var databasePath = CopyCloudDevDatabase();

        try
        {
            var provider = new ActiveCredentialProvider<CredentialPair>(new CredentialPairRepository(databasePath));
            provider.ReloadAsync().GetAwaiter().GetResult();

            if (!provider.HasActive)
                Assert.Inconclusive("No active credential pair in the Cloud dev database.");

            services.AddSingleton(provider);
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    private static string CopyCloudDevDatabase()
    {
        try
        {
            return DevCredentialsDb.CopyToTemp("Innkeep2.Cloud");
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            Assert.Inconclusive(exception.Message);
            throw;
        }
    }
}
