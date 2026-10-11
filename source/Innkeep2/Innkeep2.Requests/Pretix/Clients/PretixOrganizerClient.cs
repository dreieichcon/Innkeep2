using System.Text.Json;
using Demolite.Core.Models;
using Demolite.Http.Client;
using Innkeep2.Models.Core;
using Innkeep2.Models.Pretix;
using Innkeep2.Models.Pretix.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Innkeep2.Requests.Pretix.Clients;

public sealed class PretixOrganizerClient(
    HttpClient httpClient,
    [FromKeyedServices("pretix")] JsonSerializerOptions serializerOptions,
    ILogger<PretixOrganizerClient> logger)
    : ApiClient(httpClient, serializerOptions, logger)
{
    public Task<Result<PretixPagedResult<PretixOrganizer>>> GetAllAsync(CancellationToken ct = default)
        => GetAsync<PretixPagedResult<PretixOrganizer>>("organizers/", ct);
}
