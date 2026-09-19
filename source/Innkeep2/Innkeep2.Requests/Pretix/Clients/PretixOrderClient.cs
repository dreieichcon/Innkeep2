using System.Text.Json;
using Innkeep2.Models.Core;
using Innkeep2.Models.Pretix.Order;
using Innkeep2.Models.Pretix.Refund;
using Innkeep2.Requests.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Innkeep2.Requests.Pretix.Clients;

public sealed class PretixOrderClient(
    HttpClient httpClient,
    [FromKeyedServices("pretix")] JsonSerializerOptions serializerOptions)
    : CoreApiClient(httpClient, serializerOptions)
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