using System.Text.Json;
using Demolite.Core.Models;
using Demolite.Http.Client;
using Innkeep2.Models.Pretix.Order;
using Innkeep2.Models.Pretix.Refund;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Innkeep2.Requests.Pretix.Clients;

public sealed class PretixOrderClient(
    HttpClient httpClient,
    [FromKeyedServices("pretix")] JsonSerializerOptions serializerOptions,
    ILogger<PretixOrderClient> logger)
    : ApiClient(httpClient, serializerOptions, logger)
{
    public Task<Result<PretixOrderResponse>> CreateAsync(
        string organizerSlug,
        string eventSlug,
        PretixOrderCreate order,
        CancellationToken ct = default
    )
        => PostAsync<PretixOrderResponse>($"organizers/{organizerSlug}/events/{eventSlug}/orders/", order, ct);
    
    public Task<Result<PretixRefund>> CreateRefundAsync(
        string organizerSlug,
        string eventSlug,
        string orderCode,
        decimal amount,
        CancellationToken ct = default
    )
        => PostAsync<PretixRefund>($"organizers/{organizerSlug}/events/{eventSlug}/orders/{orderCode}/refunds/", new PretixRefundCreate
        {
            Amount = amount,
            Payment = 1,
            MarkCanceled = true
        }, ct);

    public Task<Result<PretixRefund>> MarkRefundDoneAsync(
        string organizerSlug,
        string eventSlug,
        string orderCode,
        int localId,
        CancellationToken ct = default
    )
        => PostAsync<PretixRefund>($"organizers/{organizerSlug}/events/{eventSlug}/orders/{orderCode}/refunds/{localId}/done/", null, ct);
}
