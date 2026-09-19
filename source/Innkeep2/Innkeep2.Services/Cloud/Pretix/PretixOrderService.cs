using Innkeep2.Models.Core;
using Innkeep2.Models.Internal;
using Innkeep2.Models.Pretix.Order;
using Innkeep2.Models.Pretix.Refund;
using Innkeep2.Requests.Pretix.Clients;
using JetBrains.Annotations;

namespace Innkeep2.Services.Cloud.Pretix;

[UsedImplicitly]
public sealed class PretixOrderService(PretixOrderClient client)
{
    public Task<Result<PretixOrderResponse>> CreateOrderAsync(
        string organizerSlug,
        string eventSlug,
        bool isTestMode,
        IReadOnlyList<SalesItem> items,
        CancellationToken ct = default
    )
    {
        var order = new PretixOrderCreate
        {
            Locale = "de",
            IsTestMode = isTestMode,
            Positions = BuildPositions(items)
        };

        return client.CreateAsync(organizerSlug, eventSlug, order, ct);
    }
    
    public Task<Result<PretixRefund>> CreateRefundAsync(
        string organizerSlug,
        string eventSlug,
        string orderCode,
        decimal amount,
        CancellationToken ct = default
    )
        => client.CreateRefundAsync(organizerSlug, eventSlug, orderCode, amount, ct);

    public Task<Result<PretixRefund>> MarkRefundDoneAsync(
        string organizerSlug,
        string eventSlug,
        string orderCode,
        int localId,
        CancellationToken ct = default
    )
        => client.MarkRefundDoneAsync(organizerSlug, eventSlug, orderCode, localId, ct);

    private static List<PretixOrderCreatePosition> BuildPositions(IReadOnlyList<SalesItem> items)
        => items.SelectMany(item => Enumerable.Range(0, item.Quantity ?? 1).Select(_ => item))
            .Select((item, index) => new PretixOrderCreatePosition
        {
            PositionId = index + 1,
            Item = item.Id,
            Variation = item.VariationId == 0 ? null : item.VariationId,
            Price = item.Price
        }).ToList();
    
    
}