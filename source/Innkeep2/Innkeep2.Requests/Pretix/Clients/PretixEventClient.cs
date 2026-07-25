using System.Text.Json;
using Innkeep2.Models.Core;
using Innkeep2.Models.Pretix;
using Innkeep2.Models.Pretix.Core;
using Innkeep2.Requests.Core;

namespace Innkeep2.Requests.Pretix.Clients;

public sealed class PretixEventClient(HttpClient httpClient, JsonSerializerOptions serializerOptions)
	: CoreApiClient(httpClient, serializerOptions)
{
	public Task<Result<PretixPagedResult<PretixEvent>>> GetAllAsync(
		string organizerSlug,
		CancellationToken ct = default
	)
		=> GetAsync<PretixPagedResult<PretixEvent>>($"organizers/{organizerSlug}/events/", ct);
}