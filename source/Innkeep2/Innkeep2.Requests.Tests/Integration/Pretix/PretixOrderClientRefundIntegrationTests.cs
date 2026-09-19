using Innkeep2.Models.Pretix;
using Innkeep2.Models.Pretix.Order;
using Innkeep2.Requests.Pretix.Clients;
using Microsoft.Extensions.DependencyInjection;

namespace Innkeep2.Requests.Tests.Integration.Pretix;

[TestClass]
public class PretixOrderClientRefundIntegrationTests : PretixIntegrationTestBase
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
    public async Task CreateAndRefundOrder_CancelsOrder()
    {
       var orderClient = ServiceProvider.GetRequiredService<PretixOrderClient>();

       var order = new PretixOrderCreate
       {
          Locale = "de",
          IsTestMode = true,
          Positions =
          [
             new PretixOrderCreatePosition
             {
                PositionId = 1,
                Item = _salesItem.Id,
                Price = _salesItem.DefaultPrice
             }
          ]
       };

       var createResult = await orderClient.CreateAsync(_organizer.Slug, _event.Slug, order);
       Assert.IsTrue(createResult.IsSuccess);

       var pretixOrder = createResult.Value!;

       var refundResult = await orderClient.CreateRefundAsync(
          _organizer.Slug,
          _event.Slug,
          pretixOrder.Code,
          _salesItem.DefaultPrice
       );

       Assert.IsTrue(refundResult.IsSuccess);
       Assert.AreEqual("created", refundResult.Value!.State);

       var doneResult = await orderClient.MarkRefundDoneAsync(
          _organizer.Slug,
          _event.Slug,
          pretixOrder.Code,
          refundResult.Value!.LocalId
       );

       Assert.IsTrue(doneResult.IsSuccess);
       Assert.AreEqual("done", doneResult.Value!.State);
    }
}