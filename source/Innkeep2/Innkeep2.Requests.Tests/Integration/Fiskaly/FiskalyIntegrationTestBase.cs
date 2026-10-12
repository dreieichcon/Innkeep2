using Innkeep2.Requests.Fiskaly;
using Innkeep2.TestBase;
using Microsoft.Extensions.DependencyInjection;

namespace Innkeep2.Requests.Tests.Integration.Fiskaly;

public abstract class FiskalyIntegrationTestBase
{
    protected ServiceProvider ServiceProvider { get; private set; } = null!;

    [TestInitialize]
    public void BaseTestInitialize()
    {
        var services = new ServiceCollection();
        services.AddDevCredentialPair();
        services.AddFiskalyClients();

        ServiceProvider = services.BuildServiceProvider();
    }

    [TestCleanup]
    public void BaseTestCleanup() => ServiceProvider.Dispose();


}
