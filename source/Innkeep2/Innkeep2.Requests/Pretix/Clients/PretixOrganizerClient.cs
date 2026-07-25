using System.Text.Json;
using Innkeep2.Models.Core;
using Innkeep2.Models.Pretix;
using Innkeep2.Models.Pretix.Core;
using Innkeep2.Requests.Core;

namespace Innkeep2.Requests.Pretix.Clients;

public sealed class PretixOrganizerClient(HttpClient httpClient, JsonSerializerOptions serializerOptions)
	: CoreApiClient(httpClient, serializerOptions)
{
	public Task<Result<PretixPagedResult<PretixOrganizer>>> GetAllAsync(CancellationToken ct = default)
		=> GetAsync<PretixPagedResult<PretixOrganizer>>("organizers/", ct);
}