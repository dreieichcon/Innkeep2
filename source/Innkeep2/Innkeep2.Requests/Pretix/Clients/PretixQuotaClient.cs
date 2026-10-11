using System.Text.Json;
using Demolite.Core.Models;
using Demolite.Http.Client;
using Innkeep2.Models.Core;
using Innkeep2.Models.Pretix;
using Innkeep2.Models.Pretix.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Innkeep2.Requests.Pretix.Clients;

public sealed class PretixQuotaClient(HttpClient httpClient, [FromKeyedServices("pretix")] JsonSerializerOptions serializerOptions, ILogger<PretixQuotaClient> logger)
    : ApiClient(httpClient, serializerOptions, logger)
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
