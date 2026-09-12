using System.Text.Json;
using Innkeep2.Models.Core;
using Innkeep2.Models.Pretix;
using Innkeep2.Models.Pretix.Order;
using Innkeep2.Requests.Core;

namespace Innkeep2.Requests.Pretix.Clients;

public sealed class PretixOrderClient(HttpClient httpClient, JsonSerializerOptions serializerOptions)
    : CoreApiClient(httpClient, serializerOptions)
{
    public Task<Result<PretixOrderResponse>> CreateAsync(
        string organizerSlug,
        string eventSlug,
        PretixOrderCreate order,
        CancellationToken ct = default
    )
        => PostAsync<PretixOrderResponse>($"organizers/{organizerSlug}/events/{eventSlug}/orders/", order, ct);
}