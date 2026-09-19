using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Innkeep2.Requests.Tests.Integration.Fiskaly;

public abstract class FiskalyTssIntegrationTestBase : FiskalyIntegrationTestBase
{
    protected Guid TssId { get; private set; }
    protected Guid ClientId { get; private set; }

    [TestInitialize]
    public new void BaseTestInitialize()
    {
        base.BaseTestInitialize();

        var configuration = ServiceProvider.GetRequiredService<IConfiguration>();

        TssId = Guid.Parse(configuration["fiskalyTest:tssId"]!);
        ClientId = Guid.Parse(configuration["fiskalyTest:clientId"]!);
    }
}