using Innkeep2.Models.Pretix;
using Innkeep2.Requests.Pretix.Clients;
using Microsoft.Extensions.DependencyInjection;

namespace Innkeep2.Requests.Tests.Integration.Pretix;

[TestClass]
public class PretixEventClientIntegrationTests : PretixIntegrationTestBase
{
	private PretixOrganizer _organizer = null!;
		
	[TestInitialize]
	public async Task TestInitialize()
	{
		var organizerClient = ServiceProvider.GetRequiredService<PretixOrganizerClient>();
		var organizers = await organizerClient.GetAllAsync();
		_organizer = organizers.Value!.Results.First();
	}

	[TestMethod]
	public async Task GetEventsAsync_ReturnsEvents()
	{
		var client = ServiceProvider.GetRequiredService<PretixEventClient>();
		
		var result = await client.GetAllAsync(_organizer.Slug);
		
		Assert.IsTrue(result.IsSuccess);
		Assert.IsNotNull(result.Value);
	}
}