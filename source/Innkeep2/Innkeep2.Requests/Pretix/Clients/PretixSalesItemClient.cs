using System.Text.Json;
using Innkeep2.Models.Core;
using Innkeep2.Models.Pretix;
using Innkeep2.Models.Pretix.Core;
using Innkeep2.Requests.Core;

namespace Innkeep2.Requests.Pretix.Clients;

public sealed class PretixSalesItemClient(HttpClient httpClient, JsonSerializerOptions serializerOptions)
	: CoreApiClient(httpClient, serializerOptions)
{
	public Task<Result<PretixPagedResult<PretixSalesItem>>> GetAllAsync(
		string organizerSlug,
		string eventSlug,
		CancellationToken ct = default
	)
		=> GetAsync<PretixPagedResult<PretixSalesItem>>($"organizers/{organizerSlug}/events/{eventSlug}/items/", ct);
}