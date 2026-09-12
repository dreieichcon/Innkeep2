using Innkeep2.Models.Core;
using Innkeep2.Models.Internal;
using Innkeep2.Models.Pretix;
using Innkeep2.Models.Pretix.Order;
using Innkeep2.Services.Pretix;

namespace Innkeep2.Services.Cloud.Order;

public sealed class OrderService(PretixOrderService pretixOrderService)
{
    public async Task<Result<PretixOrderResponse>> CreateOrderAsync(
        string organizerSlug,
        PretixEvent pretixEvent,
        IReadOnlyList<SalesItem> items,
        CancellationToken ct = default
    )
    {
        var orderResult = await pretixOrderService.CreateOrderAsync(organizerSlug, pretixEvent, items, ct);

        // Fiskaly-Anbindung folgt hier

        return orderResult;
    }
}