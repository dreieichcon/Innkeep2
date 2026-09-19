using System.Text.Json;
using Innkeep2.Models.Core;
using Innkeep2.Models.Pretix;
using Innkeep2.Models.Pretix.Core;
using Innkeep2.Requests.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Innkeep2.Requests.Pretix.Clients;

public sealed class PretixQuotaClient(HttpClient httpClient, [FromKeyedServices("pretix")] JsonSerializerOptions serializerOptions)
    : CoreApiClient(httpClient, serializerOptions)
{
    public Task<Result<PretixPagedResult<PretixQuota>>> GetAllAsync(
        string organizerSlug,
        string eventSlug,
        CancellationToken ct = default
    )
        => GetAsync<PretixPagedResult<PretixQuota>>(
            $"organizers/{organizerSlug}/events/{eventSlug}/quotas/?with_availability=true",
            ct
        );
}