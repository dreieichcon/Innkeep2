using Innkeep2.Models.Internal;
using Innkeep2.Models.Pretix;

namespace Innkeep2.Services.Cloud.Cache;

public static class SalesItemAvailability
{
	private const string PosSalesChannel = "pretixpos";

	/// <summary>
	/// Keeps only items and variations that are active, sold on the POS channel and inside their sales window.
	/// Items whose variations are all removed are dropped.
	/// </summary>
	public static IEnumerable<PretixSalesItem> FilterSellable(IEnumerable<PretixSalesItem> items, DateTimeOffset now)
		=> items
			.Where(x => IsSellable(x, now))
			.Select(x => x with { Variations = x.Variations.Where(v => IsSellable(v, now)).ToList() })
			.Where(x => !x.HasVariations || x.Variations.Count > 0);

	/// <summary>
	/// Sets the stock of each item from the matching quota with the least stock left.
	/// Items without a limiting quota keep an unlimited stock.
	/// </summary>
	public static void ApplyStock(List<SalesItem> items, IReadOnlyList<PretixQuota> quotas)
	{
		foreach (var item in items)
		{
			var limitingQuota = quotas
				.Where(q => AppliesTo(q, item) && q.AvailableNumber.HasValue)
				.MinBy(q => q.AvailableNumber);

			if (limitingQuota is null)
				continue;

			item.MaxStock = limitingQuota.Size;
			item.ItemsInStock = limitingQuota.AvailableNumber;
		}
	}

	/// <summary>
	/// Removes items whose quota has no stock left. Items without a quota limit are kept.
	/// </summary>
	public static void RemoveSoldOut(List<SalesItem> items)
		=> items.RemoveAll(x => x.ItemsInStock == 0);

	private static bool AppliesTo(PretixQuota quota, SalesItem item)
		=> quota.Items.Contains(item.Id)
		   && (item.VariationId == 0 || quota.Variations.Contains(item.VariationId));

	private static bool IsSellable(PretixSalesItem item,DateTimeOffset now)
		=> item.Active
		   && IsOnPosChannel(item.AllSalesChannels, item.LimitSalesChannels)
		   && IsInSalesWindow(item.AvailableFrom, item.AvailableUntil, now);

	private static bool IsSellable(PretixItemVariation variation, DateTimeOffset now)
		=> variation.Active
		   && IsOnPosChannel(variation.AllSalesChannels, variation.LimitSalesChannels)
		   && IsInSalesWindow(variation.AvailableFrom, variation.AvailableUntil, now);

	private static bool IsOnPosChannel(bool allSalesChannels, IReadOnlyList<string> limitSalesChannels)
		=> allSalesChannels || limitSalesChannels.Contains(PosSalesChannel);

	private static bool IsInSalesWindow(DateTimeOffset? availableFrom, DateTimeOffset? availableUntil, DateTimeOffset now)
		=> (availableFrom is null || availableFrom <= now)
		   && (availableUntil is null || availableUntil >= now);
}
