using Innkeep2.Models.Pretix;
using Innkeep2.Models.Pretix.Order;
using Innkeep2.Requests.Pretix.Clients;
using Microsoft.Extensions.DependencyInjection;

namespace Innkeep2.Requests.Tests.Integration.Pretix;

[TestClass]
public class PretixOrderClientIntegrationTests : PretixIntegrationTestBase
{
    private PretixOrganizer _organizer = null!;
    private PretixEvent _event = null!;
    private PretixSalesItem _salesItem = null!;

    [TestInitialize]
    public async Task TestInitialize()
    {
        var organizerClient = ServiceProvider.GetRequiredService<PretixOrganizerClient>();
        var organizers = await organizerClient.GetAllAsync();
        _organizer = organizers.Value!.Results.First();

        var eventClient = ServiceProvider.GetRequiredService<PretixEventClient>();
        var events = await eventClient.GetAllAsync(_organizer.Slug);
        _event = events.Value!.Results.First();

        var salesItemClient = ServiceProvider.GetRequiredService<PretixSalesItemClient>();
        var salesItems = await salesItemClient.GetAllAsync(_organizer.Slug, _event.Slug);
        _salesItem = salesItems.Value!.Results.First();
    }

    [TestMethod]
    public async Task CreateAsync_ReturnsOrder()
    {
        var client = ServiceProvider.GetRequiredService<PretixOrderClient>();

        var order = new PretixOrderCreate
        {
            Locale = "de",
            IsTestMode = true,
            Positions =
            [
                new PretixOrderCreatePosition()
                {
                    PositionId = 1,
                    Item = _salesItem.Id,
                    Price = _salesItem.DefaultPrice
                }
            ]
        };

        var result = await client.CreateAsync(_organizer.Slug, _event.Slug, order);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual("p", result.Value!.Status);
    }
}