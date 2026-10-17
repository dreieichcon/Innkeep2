using Innkeep2.Models.Internal;
using Innkeep2.Models.Pretix;
using Innkeep2.Models.Pretix.Core;
using Innkeep2.Services.Cloud.Cache;

namespace Innkeep2.Cloud.Tests.Unit;

[TestClass]
public class SalesItemAvailabilityTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void FilterSellable_ActiveItemWithoutWindow_IsKept()
    {
        var result = SalesItemAvailability.FilterSellable([CreateItem()], Now).ToList();

        Assert.HasCount(1, result);
    }

    [TestMethod]
    public void FilterSellable_InactiveItem_IsRemoved()
    {
        var result = SalesItemAvailability.FilterSellable([CreateItem(active: false)], Now).ToList();

        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void FilterSellable_ItemNotOnPosChannel_IsRemoved()
    {
        var item = CreateItem(allSalesChannels: false, channels: ["web"]);

        var result = SalesItemAvailability.FilterSellable([item], Now).ToList();

        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void FilterSellable_ItemLimitedToPosChannel_IsKept()
    {
        var item = CreateItem(allSalesChannels: false, channels: ["pretixpos"]);

        var result = SalesItemAvailability.FilterSellable([item], Now).ToList();

        Assert.HasCount(1, result);
    }

    [TestMethod]
    public void FilterSellable_ItemNotYetAvailable_IsRemoved()
    {
        var item = CreateItem(availableFrom: Now.AddHours(1));

        var result = SalesItemAvailability.FilterSellable([item], Now).ToList();

        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void FilterSellable_ItemNoLongerAvailable_IsRemoved()
    {
        var item = CreateItem(availableUntil: Now.AddHours(-1));

        var result = SalesItemAvailability.FilterSellable([item], Now).ToList();

        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void FilterSellable_ItemInsideWindow_IsKept()
    {
        var item = CreateItem(availableFrom: Now.AddHours(-1), availableUntil: Now.AddHours(1));

        var result = SalesItemAvailability.FilterSellable([item], Now).ToList();

        Assert.HasCount(1, result);
    }

    [TestMethod]
    public void FilterSellable_VariationsDiffer_OnlySellableVariationsRemain()
    {
        var item = CreateItem(variations:
        [
            CreateVariation(1),
            CreateVariation(2, active: false),
            CreateVariation(3, availableUntil: Now.AddHours(-1)),
            CreateVariation(4, availableFrom: Now.AddHours(1))
        ]);

        var result = SalesItemAvailability.FilterSellable([item], Now).Single();

        CollectionAssert.AreEqual(new[] { 1 }, result.Variations.Select(x => x.Id).ToArray());
    }

    [TestMethod]
    public void FilterSellable_AllVariationsUnsellable_ItemIsRemoved()
    {
        var item = CreateItem(variations: [CreateVariation(1, active: false)]);

        var result = SalesItemAvailability.FilterSellable([item], Now).ToList();

        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void RemoveSoldOut_StockZero_IsRemoved()
    {
        var items = new List<SalesItem> { CreateSalesItem(1, stock: 0), CreateSalesItem(2, stock: 3) };

        SalesItemAvailability.RemoveSoldOut(items);

        CollectionAssert.AreEqual(new[] { 2 }, items.Select(x => x.Id).ToArray());
    }

    [TestMethod]
    public void RemoveSoldOut_NoStockLimit_IsKept()
    {
        var items = new List<SalesItem> { CreateSalesItem(1, stock: null) };

        SalesItemAvailability.RemoveSoldOut(items);

        Assert.HasCount(1, items);
    }

    private static SalesItem CreateSalesItem(int id, int? stock)
        => new()
        {
            Id = id,
            VariationId = 0,
            Name = "Item",
            Price = 1,
            TaxRate = 19,
            ItemsInStock = stock
        };

    private static PretixSalesItem CreateItem(
        bool active = true,
        bool allSalesChannels = true,
        string[]? channels = null,
        DateTimeOffset? availableFrom = null,
        DateTimeOffset? availableUntil = null,
        PretixItemVariation[]? variations = null
    )
        => new()
        {
            Id = 1,
            Name = new MultiLanguageString { ["de"] = "Item" },
            DefaultPrice = 1,
            Active = active,
            FreePrice = false,
            TaxRate = 19,
            Admission = false,
            Personalized = false,
            Position = 0,
            AllSalesChannels = allSalesChannels,
            LimitSalesChannels = channels ?? [],
            AvailableFrom = availableFrom,
            AvailableFromMode = "hide",
            AvailableUntil = availableUntil,
            AvailableUntilMode = "hide",
            HiddenIfItemAvailableMode = "hide",
            RequireVoucher = false,
            HideWithoutVoucher = false,
            AllowCancel = true,
            CheckinAttention = false,
            RequireApproval = false,
            RequireBundling = false,
            RequireMembership = false,
            RequireMembershipHidden = false,
            RequireMembershipTypes = [],
            GrantMembershipDurationLikeEvent = false,
            GrantMembershipDurationDays = 0,
            GrantMembershipDurationMonths = 0,
            ValidityDynamicStartChoice = false,
            AllowWaitinglist = false,
            IssueGiftcard = false,
            HasVariations = variations is { Length: > 0 },
            Variations = variations ?? [],
            MetaData = new Dictionary<string, object?>()
        };

    private static PretixItemVariation CreateVariation(
        int id,
        bool active = true,
        DateTimeOffset? availableFrom = null,
        DateTimeOffset? availableUntil = null
    )
        => new()
        {
            Id = id,
            Name = new MultiLanguageString { ["de"] = "Variation" },
            Price = 1,
            Active = active,
            CheckinAttention = false,
            RequireApproval = false,
            RequireMembership = false,
            RequireMembershipHidden = false,
            RequireMembershipTypes = [],
            AllSalesChannels = true,
            LimitSalesChannels = [],
            AvailableFrom = availableFrom,
            AvailableFromMode = "hide",
            AvailableUntil = availableUntil,
            AvailableUntilMode = "hide",
            HideWithoutVoucher = false,
            MetaData = new Dictionary<string, object?>(),
            Position = 0
        };
}
