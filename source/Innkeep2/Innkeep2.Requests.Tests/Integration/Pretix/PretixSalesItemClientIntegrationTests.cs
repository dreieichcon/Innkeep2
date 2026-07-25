using Innkeep2.Models.Pretix;
using Innkeep2.Requests.Pretix.Clients;
using Microsoft.Extensions.DependencyInjection;

namespace Innkeep2.Requests.Tests.Integration.Pretix;

[TestClass]
public class PretixSalesItemClientIntegrationTests : PretixIntegrationTestBase
{
	private PretixOrganizer _organizer = null!;
	private PretixEvent _event = null!;
		
	[TestInitialize]
	public async Task TestInitialize()
	{
		var organizerClient = ServiceProvider.GetRequiredService<PretixOrganizerClient>();
		var eventClient = ServiceProvider.GetRequiredService<PretixEventClient>();
		
		var organizers = await organizerClient.GetAllAsync();
		_organizer = organizers.Value!.Results.First();
		
		var events = await eventClient.GetAllAsync(_organizer.Slug);
		_event = events.Value!.Results.First();
	}
	
	[TestMethod]
	public async Task GetSalesItemsAsync_ReturnsSalesItems()
	{
		var salesItemClient = ServiceProvider.GetRequiredService<PretixSalesItemClient>();
		
		var result = await salesItemClient.GetAllAsync(_organizer.Slug, _event.Slug);
		
		Assert.IsTrue(result.IsSuccess);
		Assert.IsNotNull(result.Value);
	}
}