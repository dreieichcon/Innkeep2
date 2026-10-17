using Innkeep2.Models.Internal;
using Innkeep2.Models.Pretix;
using Innkeep2.Services.Cloud.Cache;

namespace Innkeep2.Cloud.Tests.Unit;

[TestClass]
public class SalesItemStockTests
{
    [TestMethod]
    public void ApplyStock_SingleQuota_SetsStockAndMaxStock()
    {
        var items = new List<SalesItem> { CreateSalesItem(1) };

        SalesItemAvailability.ApplyStock(items, [CreateQuota(items: [1], size: 10, available: 4)]);

        Assert.AreEqual(4, items[0].ItemsInStock);
        Assert.AreEqual(10, items[0].MaxStock);
    }

    [TestMethod]
    public void ApplyStock_MultipleQuotas_UsesQuotaWithLeastStock()
    {
        var items = new List<SalesItem> { CreateSalesItem(1) };
        var quotas = new[]
        {
            CreateQuota(items: [1], size: 50, available: 20),
            CreateQuota(items: [1], size: 5, available: 0)
        };

        SalesItemAvailability.ApplyStock(items, quotas);
        SalesItemAvailability.RemoveSoldOut(items);

        Assert.IsEmpty(items);
    }

    [TestMethod]
    public void ApplyStock_UnlimitedQuota_KeepsStockUnlimited()
    {
        var items = new List<SalesItem> { CreateSalesItem(1) };

        SalesItemAvailability.ApplyStock(items, [CreateQuota(items: [1], size: null, available: null)]);

        Assert.IsNull(items[0].ItemsInStock);
    }

    [TestMethod]
    public void ApplyStock_QuotaOfOtherVariation_IsIgnored()
    {
        var items = new List<SalesItem> { CreateSalesItem(1, variationId: 2) };

        SalesItemAvailability.ApplyStock(items, [CreateQuota(items: [1], variations: [3], size: 5, available: 0)]);

        Assert.IsNull(items[0].ItemsInStock);
    }

    private static SalesItem CreateSalesItem(int id, int variationId = 0)
        => new()
        {
            Id = id,
            VariationId = variationId,
            Name = "Item",
            Price = 1,
            TaxRate = 19
        };

    private static PretixQuota CreateQuota(int[] items, int? size, int? available, int[]? variations = null)
        => new()
        {
            Id = 1,
            Size = size,
            Items = items,
            Variations = variations ?? [],
            AvailableNumber = available
        };
}
