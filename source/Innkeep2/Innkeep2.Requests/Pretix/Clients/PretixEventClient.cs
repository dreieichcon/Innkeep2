using System.Text.Json;
using Demolite.Core.Models;
using Demolite.Http.Client;
using Innkeep2.Models.Core;
using Innkeep2.Models.Pretix;
using Innkeep2.Models.Pretix.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Innkeep2.Requests.Pretix.Clients;

public sealed class PretixEventClient(
    HttpClient httpClient,
    [FromKeyedServices("pretix")] JsonSerializerOptions serializerOptions,
    ILogger<PretixEventClient> logger)
    : ApiClient(httpClient, serializerOptions, logger)
{
    public Task<Result<PretixPagedResult<PretixEvent>>> GetAllAsync(
        string organizerSlug,
        CancellationToken ct = default
    )
        => GetAsync<PretixPagedResult<PretixEvent>>($"organizers/{organizerSlug}/events/", ct);
}
