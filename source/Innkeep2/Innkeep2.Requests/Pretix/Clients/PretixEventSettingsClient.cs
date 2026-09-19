using System.Text.Json;
using Innkeep2.Models.Core;
using Innkeep2.Models.Pretix;
using Innkeep2.Requests.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Innkeep2.Requests.Pretix.Clients;

public sealed class PretixEventSettingsClient(HttpClient httpClient, [FromKeyedServices("pretix")] JsonSerializerOptions serializerOptions)
    : CoreApiClient(httpClient, serializerOptions)
{
    public Task<Result<PretixEventSettings>> GetAsync(string organizerSlug, string eventSlug, CancellationToken ct = default)
        => GetAsync<PretixEventSettings>($"organizers/{organizerSlug}/events/{eventSlug}/settings/", ct);
}