using Innkeep2.Credentials;
using Innkeep2.Requests.Fiskaly;
using Innkeep2.TestBase;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Innkeep2.Requests.Tests.Integration.Fiskaly;

public abstract class FiskalyIntegrationTestBase
{
    protected ServiceProvider ServiceProvider { get; private set; } = null!;

    [TestInitialize]
    public void BaseTestInitialize()
    {
        var credentialsPath = CredentialsPathResolver.ResolveCredentialsPath();

        var configuration = new ConfigurationBuilder()
            .AddJsonFile(credentialsPath, optional: false)
            .AddEnvironmentVariables()
            .Build();

        var services = new ServiceCollection();
        services.AddCredentialsConfiguration(configuration);
        services.AddSingleton<IConfiguration>(configuration);
        services.AddFiskalyClients();

        ServiceProvider = services.BuildServiceProvider();
    }

    [TestCleanup]
    public void BaseTestCleanup() => ServiceProvider.Dispose();

    
}