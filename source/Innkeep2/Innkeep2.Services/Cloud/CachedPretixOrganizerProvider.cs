using Inkeep2.Services.Core;
using Innkeep2.Models.Core;
using Innkeep2.Models.Internal;
using Innkeep2.Models.Pretix;
using Innkeep2.Models.Pretix.Core;
using Innkeep2.Requests.Pretix.Clients;
using Microsoft.Extensions.Caching.Memory;

namespace Innkeep2.Services.Cloud;

public sealed class CachedPretixOrganizerProvider(PretixOrganizerClient client, IMemoryCache cache)
	: AbstractItemProvider<PretixOrganizer, Organizer>(cache)
{
	protected override TimeSpan CacheDuration => TimeSpan.FromHours(1);
	protected override string Key => "pretix_organizers";

	protected override Task<Result<PretixPagedResult<PretixOrganizer>>> FetchAsync(CancellationToken ct)
		=> client.GetAllAsync(ct);

	protected override Organizer Map(PretixOrganizer item) => Organizer.FromPretix(item);
}