using System.Text.Json;
using Demolite.Core.Models;
using Demolite.Http.Client;
using Innkeep2.Models.Pretix;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Innkeep2.Requests.Pretix.Clients;

public sealed class PretixEventSettingsClient(HttpClient httpClient, [FromKeyedServices("pretix")] JsonSerializerOptions serializerOptions, ILogger<PretixEventSettingsClient> logger)
    : ApiClient(httpClient, serializerOptions, logger)
{
    public Task<Result<PretixEventSettings>> GetAsync(string organizerSlug, string eventSlug, CancellationToken ct = default)
        => GetAsync<PretixEventSettings>($"organizers/{organizerSlug}/events/{eventSlug}/settings/", ct);
}
