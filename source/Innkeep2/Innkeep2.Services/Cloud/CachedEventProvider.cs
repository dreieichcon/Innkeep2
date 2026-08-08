using Innkeep2.Models.Core;
using Innkeep2.Models.Internal;
using Innkeep2.Models.Pretix;
using Innkeep2.Models.Pretix.Core;
using Innkeep2.Requests.Pretix.Clients;
using Innkeep2.Services.Core;
using Microsoft.Extensions.Caching.Memory;

namespace Innkeep2.Services.Cloud;

public sealed record EventKey(string OrganizerSlug);

public sealed class CachedEventProvider(PretixEventClient client, IMemoryCache cache) : AbstractItemProvider<EventKey, PretixEvent, Event>(cache)
{
	protected override TimeSpan CacheDuration => TimeSpan.FromHours(1);

	protected override string BuildKey(EventKey context) =>
		$"pretix_events:{context.OrganizerSlug}";
	
	protected override Task<Result<PretixPagedResult<PretixEvent>>> FetchAsync(EventKey context, CancellationToken ct)
		=> client.GetAllAsync(context.OrganizerSlug, ct);

	protected override IEnumerable<Event> Map(PretixEvent item)
		=> [Event.FromPretix(item)];
}