using Innkeep2.Models.Core;
using Innkeep2.Models.Internal;
using Innkeep2.Models.Pretix;
using Innkeep2.Models.Pretix.Order;
using Innkeep2.Requests.Pretix.Clients;

namespace Innkeep2.Services.Pretix;

public sealed class PretixOrderService(PretixOrderClient client)
{
    /// <summary>
    /// Erstellt eine Bestellung aus den übergebenen Artikeln für das angegebene Event.
    /// </summary>
    public Task<Result<PretixOrderResponse>> CreateOrderAsync(
        string organizerSlug,
        PretixEvent pretixEvent,
        IReadOnlyList<SalesItem> items,
        CancellationToken ct = default
    )
    {
        var order = new PretixOrderCreate
        {
            Locale = "de",
            IsTestMode = pretixEvent.TestMode,
            Positions = BuildPositions(items)
        };

        return client.CreateAsync(organizerSlug, pretixEvent.Slug, order, ct);
    }

    private static List<PretixOrderCreatePosition> BuildPositions(IReadOnlyList<SalesItem> items)
        => items.Select((item, index) => new PretixOrderCreatePosition
        {
            PositionId = index + 1,
            Item = item.Id,
            Variation = item.VariationId == 0 ? null : item.VariationId,
            Price = item.Price
        }).ToList();
}