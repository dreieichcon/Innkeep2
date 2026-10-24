using Demolite.Core.Models;
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
		if (cache.TryGetValue(BuildKey(context), out IReadOnlyList<SalesItem>? cached))
			return Result<IReadOnlyList<SalesItem>>.Success(cached!);

		return await LoadAsync(context, ct);
	}

	/// <summary>
	/// Drops the cache and loads items and stock from Pretix again.
	/// </summary>
	public async Task<Result<IReadOnlyList<SalesItem>>> RefreshAsync(SalesItemKey context, CancellationToken ct = default)
	{
		Invalidate(context);

		return await LoadAsync(context, ct);
	}

	/// <summary>
	/// Loads only the quotas from Pretix and applies them to the cached items.
	/// Loads everything when nothing is cached yet.
	/// </summary>
	public async Task<Result<IReadOnlyList<SalesItem>>> RefreshStockAsync(SalesItemKey context, CancellationToken ct = default)
	{
		if (!cache.TryGetValue(BuildKey(context), out IReadOnlyList<SalesItem>? cached))
			return await LoadAsync(context, ct);

		var quotasResult = await quotaClient.GetAllAsync(context.OrganizerSlug, context.EventSlug, ct);

		if (!quotasResult.IsSuccess)
			return Result<IReadOnlyList<SalesItem>>.Failure(quotasResult.Error!);

		var items = cached!.Select(x => x with { }).ToList();

		SalesItemAvailability.ApplyStock(items, quotasResult.Value!.Results);
		SalesItemAvailability.RemoveSoldOut(items);

		cache.Set(BuildKey(context), (IReadOnlyList<SalesItem>)items, CacheDuration);

		return Result<IReadOnlyList<SalesItem>>.Success(items);
	}

	public void Invalidate(SalesItemKey context)
		=> cache.Remove(BuildKey(context));

	private async Task<Result<IReadOnlyList<SalesItem>>> LoadAsync(SalesItemKey context, CancellationToken ct)
	{
		var itemsResult = await salesItemClient.GetAllAsync(context.OrganizerSlug, context.EventSlug, ct);

		if (!itemsResult.IsSuccess)
			return Result<IReadOnlyList<SalesItem>>.Failure(itemsResult.Error!);

		var quotasResult = await quotaClient.GetAllAsync(context.OrganizerSlug, context.EventSlug, ct);

		if (!quotasResult.IsSuccess)
			return Result<IReadOnlyList<SalesItem>>.Failure(quotasResult.Error!);

		var items = SalesItemAvailability.FilterSellable(itemsResult.Value!.Results, DateTimeOffset.UtcNow)
			.OrderBy(x => x.Category ?? int.MinValue)
			.ThenBy(x => x.Position)
			.ThenBy(x => x.Id)
			.SelectMany(SalesItem.FromPretix)
			.ToList();

		SalesItemAvailability.ApplyStock(items, quotasResult.Value!.Results);
		SalesItemAvailability.RemoveSoldOut(items);

		cache.Set(BuildKey(context), (IReadOnlyList<SalesItem>)items, CacheDuration);

		return Result<IReadOnlyList<SalesItem>>.Success(items);
	}

	private static string BuildKey(SalesItemKey context)
		=> $"pretix_sales_items:{context.OrganizerSlug}:{context.EventSlug}";
}

public sealed record SalesItemKey(string OrganizerSlug, string EventSlug);
