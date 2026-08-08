using Innkeep2.Models.Core;
using Innkeep2.Models.Internal;
using Innkeep2.Models.Pretix;
using Innkeep2.Models.Pretix.Core;
using Innkeep2.Requests.Pretix.Clients;
using Innkeep2.Services.Core;
using Microsoft.Extensions.Caching.Memory;

namespace Innkeep2.Services.Cloud;

public sealed record SalesItemKey(string OrganizerSlug, string EventSlug);

public sealed class CachedSalesItemProvider(PretixSalesItemClient client, IMemoryCache cache)
	: AbstractItemProvider<SalesItemKey, PretixSalesItem, SalesItem>(cache)
{
	protected override TimeSpan CacheDuration => TimeSpan.FromSeconds(120);

	protected override bool FilterFunc(PretixSalesItem item)
		=> item.AllSalesChannels || item.LimitSalesChannels.Contains("pretixpos");

	protected override string BuildKey(SalesItemKey context)
		=> $"pretix_sales_items:{context.OrganizerSlug}:{context.EventSlug}";

	protected override Task<Result<PretixPagedResult<PretixSalesItem>>> FetchAsync(
		SalesItemKey context,
		CancellationToken ct
	)
		=> client.GetAllAsync(context.OrganizerSlug, context.EventSlug, ct);

	protected override IEnumerable<SalesItem> Map(PretixSalesItem item)
		=> SalesItem.FromPretix(item);
}