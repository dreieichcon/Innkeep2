using Innkeep2.Models.Core;
using Innkeep2.Models.Internal;
using Innkeep2.Models.Pretix;
using Innkeep2.Models.Pretix.Core;
using Innkeep2.Requests.Core;
using Innkeep2.Requests.Pretix.Clients;
using Innkeep2.Services.Core;
using Microsoft.Extensions.Caching.Memory;

namespace Innkeep2.Services.Cloud;

public sealed class CachedOrganizerProvider(PretixOrganizerClient client, IMemoryCache cache)
	: AbstractItemProvider<Unit, PretixOrganizer, Organizer>(cache)
{
	protected override TimeSpan CacheDuration => TimeSpan.FromHours(1);

	protected override string BuildKey(Unit context) => "pretix_organizers";

	protected override Task<Result<PretixPagedResult<PretixOrganizer>>> FetchAsync(Unit context, CancellationToken ct)
		=> client.GetAllAsync(ct);

	protected override IEnumerable<Organizer> Map(PretixOrganizer item) => [Organizer.FromPretix(item)];
}