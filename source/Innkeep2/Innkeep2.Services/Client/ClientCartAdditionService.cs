using Innkeep2.Models.Internal;
using Innkeep2.Services.Shared;

namespace Innkeep2.Services.Client;

public sealed class ClientCartAdditionService(
    ClientCartService cartService,
    ClientSalesItemProvider salesItemProvider,
    LoadingService loadingService)
{
    /// <summary>
    /// Adds the item to the cart. Items with limited stock are checked against fresh stock first.
    /// </summary>
    public async Task<AddItemResult> AddAsync(SalesItem item, CancellationToken ct = default)
    {
        var amount = cartService.NextItemCount;

        if (!SalesItemStockRules.IsLimited(item))
            return Add(item, amount);

        if (SalesItemStockRules.NeedsStockRefresh(item, cartService.Items.Count == 0, salesItemProvider.IsStockFresh)
            && !await RefreshStockAsync(ct))
            return AddItemResult.Blocked("Server oder Cloud nicht erreichbar, Artikel mit Bestand können nicht verkauft werden");

        var currentItems = (await salesItemProvider.GetCachedItemsAsync(ct)).Value ?? [];
        var amountInCart = cartService.Items
            .Where(x => x.Id == item.Id && x.VariationId == item.VariationId)
            .Sum(x => x.Quantity ?? 1);

        var check = SalesItemStockRules.Check(currentItems, item, amountInCart, amount);

        if (check.Allowed)
            return Add(check.CurrentItem!, amount);

        return AddItemResult.Blocked(BuildBlockedMessage(item, check));
    }

    private AddItemResult Add(SalesItem item, int amount)
    {
        cartService.AddItem(item, amount);
        cartService.NextItemCount = 1;

        return AddItemResult.Success;
    }

    private async Task<bool> RefreshStockAsync(CancellationToken ct)
    {
        loadingService.IsLoading = true;

        try
        {
            var result = await salesItemProvider.RefreshStockAsync(ct);

            return result.IsSuccess && result.Value!.FromCloud;
        }
        finally
        {
            loadingService.IsLoading = false;
        }
    }

    private static string BuildBlockedMessage(SalesItem item, StockCheckResult check)
    {
        if (check.CurrentItem is null)
            return $"{item.Name} ist nicht mehr verfügbar";

        return check.Available == 0
            ? $"Kein weiterer Bestand für {item.Name} verfügbar"
            : $"Nur noch {check.Available} von {item.Name} verfügbar";
    }
}

public sealed record AddItemResult(bool Added, string? Message)
{
    public static AddItemResult Success { get; } = new(true, null);

    public static AddItemResult Blocked(string message) => new(false, message);
}
