using Innkeep2.TestBase;

namespace Innkeep2.Requests.Tests.Integration.Fiskaly;

public abstract class FiskalyTssIntegrationTestBase : FiskalyIntegrationTestBase
{
    protected Guid TssId { get; private set; }
    protected Guid ClientId { get; private set; }

    [TestInitialize]
    public new void BaseTestInitialize()
    {
        base.BaseTestInitialize();

        var testData = TestData.Load();

        TssId = testData.FiskalyTssId;
        ClientId = testData.FiskalyClientId;
    }
}
