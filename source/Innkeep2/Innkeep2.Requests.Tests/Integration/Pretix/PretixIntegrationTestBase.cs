using Innkeep2.Requests.Pretix;
using Innkeep2.TestBase;
using Microsoft.Extensions.DependencyInjection;

namespace Innkeep2.Requests.Tests.Integration.Pretix;

public abstract class PretixIntegrationTestBase
{
	protected ServiceProvider ServiceProvider { get; private set; } = null!;

	[TestInitialize]
	public void BaseTestInitialize()
	{
		var services = new ServiceCollection();
		services.AddDevCredentialPair();
		services.AddPretixClients();

		ServiceProvider = services.BuildServiceProvider();
	}

	[TestCleanup]
	public void BaseTestCleanup() => ServiceProvider?.Dispose();

}
