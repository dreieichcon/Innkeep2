using Innkeep2.Models.Core;
using Innkeep2.Models.Internal;
using Innkeep2.Models.Pretix;
using Innkeep2.Requests.Pretix.Clients;
using Microsoft.Extensions.Caching.Memory;

namespace Innkeep2.Services.Cloud.Cache;

public sealed class CachedSalesItemProvider(
	PretixSalesItemClient salesItemClient,
	PretixQuotaClient quotaClient,
	IMemoryCache cache
)
{
	private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(120);

	public async Task<Result<IReadOnlyList<SalesItem>>> GetCachedItemsAsync(SalesItemKey context, CancellationToken ct = default)
	{
		var key = $"pretix_sales_items:{context.OrganizerSlug}:{context.EventSlug}";

		if (cache.TryGetValue(key, out IReadOnlyList<SalesItem>? cached))
			return Result<IReadOnlyList<SalesItem>>.Success(cached!);

		var itemsResult = await salesItemClient.GetAllAsync(context.OrganizerSlug, context.EventSlug, ct);

		if (!itemsResult.IsSuccess)
			return Result<IReadOnlyList<SalesItem>>.Failure(itemsResult.Error!);

		var quotasResult = await quotaClient.GetAllAsync(context.OrganizerSlug, context.EventSlug, ct);

		if (!quotasResult.IsSuccess)
			return Result<IReadOnlyList<SalesItem>>.Failure(quotasResult.Error!);

		var items = itemsResult.Value!.Results
			.Where(x => x.AllSalesChannels || x.LimitSalesChannels.Contains("pretixpos"))
			.SelectMany(SalesItem.FromPretix)
			.ToList();

		ApplyStock(items, quotasResult.Value!.Results);

		cache.Set(key, items, CacheDuration);

		return Result<IReadOnlyList<SalesItem>>.Success(items);
	}

	private static void ApplyStock(List<SalesItem> items, IReadOnlyList<PretixQuota> quotas)
	{
		foreach (var item in items)
		{
			var quota = quotas.FirstOrDefault(q =>
				q.Items.Contains(item.Id) &&
				(item.VariationId == 0 || q.Variations.Contains(item.VariationId)));

			if (quota is null)
				continue;

			item.MaxStock = quota.Size;
			item.ItemsInStock = quota.AvailableNumber;
		}
	}
}

public sealed record SalesItemKey(string OrganizerSlug, string EventSlug);