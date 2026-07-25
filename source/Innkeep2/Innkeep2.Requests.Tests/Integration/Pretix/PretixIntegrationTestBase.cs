using Innkeep2.Credentials;
using Innkeep2.Requests.Pretix;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Innkeep2.Requests.Tests.Integration.Pretix;

public abstract class PretixIntegrationTestBase
{
	protected ServiceProvider ServiceProvider { get; private set; } = null!;
	
	[TestInitialize]
	public void BaseTestInitialize()
	{
		var credentialsPath = ResolveCredentialsPath();

		var configuration = new ConfigurationBuilder()
			.AddJsonFile(credentialsPath, optional: false)
			.AddEnvironmentVariables()
			.Build();

		var services = new ServiceCollection();
		services.AddCredentialsConfiguration(configuration);
		services.AddPretixClients(configuration);

		ServiceProvider = services.BuildServiceProvider();
	}

	[TestCleanup]
	public void BaseTestCleanup() => ServiceProvider?.Dispose();
	
	private static string ResolveCredentialsPath()
	{
		var directory = Environment.GetEnvironmentVariable("INNKEEP2_CREDENTIALS_DIR")
						?? Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "credentials");

		var path = Path.Combine(directory, "credentials.test.json");

		return File.Exists(path)
			? path
			: throw new FileNotFoundException(
				$"Credentials file not found at '{path}'. Set INNKEEP2_CREDENTIALS_DIR to override.", path);
	}
}