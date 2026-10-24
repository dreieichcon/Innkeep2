namespace Innkeep2.Models.Internal;

public sealed record SalesItemsResult(IReadOnlyList<SalesItem> Items, bool FromCloud);
