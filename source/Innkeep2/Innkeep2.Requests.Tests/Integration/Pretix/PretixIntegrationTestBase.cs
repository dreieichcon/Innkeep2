using Innkeep2.Credentials;
using Innkeep2.Requests.Pretix;
using Innkeep2.TestBase;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Innkeep2.Requests.Tests.Integration.Pretix;

public abstract class PretixIntegrationTestBase
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
		services.AddPretixClients();

		ServiceProvider = services.BuildServiceProvider();
	}

	[TestCleanup]
	public void BaseTestCleanup() => ServiceProvider?.Dispose();
	
}