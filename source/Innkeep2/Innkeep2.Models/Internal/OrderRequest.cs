using Innkeep2.Models.Shared;

namespace Innkeep2.Models.Internal;

public sealed record OrderRequest
{
    public required Guid RequestId { get; init; }
    public required IReadOnlyList<SalesItem> Items { get; init; }
    public required PaymentType PaymentType { get; init; }
    public required decimal AmountGiven { get; init; }
    public required string Currency { get; init; }

    public decimal AmountNeeded => Items.Sum(x => x.Price * (x.Quantity ?? 1));
    
    public decimal AmountBack => AmountNeeded - AmountGiven;
}