using Innkeep2.Models.Internal;

namespace Innkeep2.Services.Client;

public static class SalesItemStockRules
{
    public const int CriticalStockLimit = 20;

    public static bool IsLimited(SalesItem item)
        => item.ItemsInStock.HasValue;

    /// <summary>
    /// A limited item needs fresh stock when the last refresh was not from Cloud,
    /// or when a new cart starts with an item that is running low.
    /// </summary>
    public static bool NeedsStockRefresh(SalesItem item, bool cartIsEmpty, bool? isStockFresh)
        => IsLimited(item)
           && (isStockFresh != true || (cartIsEmpty && item.ItemsInStock < CriticalStockLimit));

    /// <summary>
    /// Checks the amount to add against the stock of the item in the current list.
    /// </summary>
    public static StockCheckResult Check(IReadOnlyList<SalesItem> currentItems, SalesItem item, int amountInCart, int amountToAdd)
    {
        var current = currentItems.FirstOrDefault(x => x.Id == item.Id && x.VariationId == item.VariationId);

        if (current is null || current.ItemsInStock == 0)
            return new StockCheckResult(false, null, 0);

        if (current.ItemsInStock is not { } stock)
            return new StockCheckResult(true, current, int.MaxValue);

        var available = Math.Max(stock - amountInCart, 0);

        return new StockCheckResult(amountToAdd <= available, current, available);
    }
}

public sealed record StockCheckResult(bool Allowed, SalesItem? CurrentItem, int Available);
