using Innkeep2.Requests.Pretix.Clients;
using Microsoft.Extensions.DependencyInjection;

namespace Innkeep2.Requests.Tests.Integration.Pretix;

[TestClass]
public sealed class PretixOrganizerClientIntegrationTests : PretixIntegrationTestBase
{
	[TestMethod]
	public async Task GetAllAsync_ReturnsOrganizers()
	{
		var client = ServiceProvider.GetRequiredService<PretixOrganizerClient>();

		var result = await client.GetAllAsync();

		Assert.IsTrue(result.IsSuccess);
		Assert.IsNotEmpty(result.Value!.Results);
	}
}