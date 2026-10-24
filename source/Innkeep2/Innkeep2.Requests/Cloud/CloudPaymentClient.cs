using System.Text.Json;
using Demolite.Core.Models;
using Demolite.Http.Client;
using Innkeep2.Models.Internal;
using Microsoft.Extensions.Logging;

namespace Innkeep2.Requests.Cloud;

public sealed class CloudPaymentClient(HttpClient httpClient, ILogger<CloudPaymentClient> logger)
    : ApiClient(httpClient, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, logger), ICloudPaymentClient
{
    /// <summary>
    /// Starts a card payment on the reader of the Cloud.
    /// </summary>
    public Task<Result<PaymentStatusResponse>> StartCardPaymentAsync(OrderRequest request, CancellationToken ct = default)
        => PostAsync<PaymentStatusResponse>("payments/card/start", request, ct);

    /// <summary>
    /// Loads the current status of a card payment.
    /// </summary>
    public Task<Result<PaymentStatusResponse>> GetCardPaymentAsync(Guid requestId, CancellationToken ct = default)
        => GetAsync<PaymentStatusResponse>($"payments/card/{requestId}", ct);

    /// <summary>
    /// Asks the Cloud to cancel a pending card payment.
    /// </summary>
    public Task<Result<PaymentStatusResponse>> CancelCardPaymentAsync(Guid requestId, CancellationToken ct = default)
        => PostAsync<PaymentStatusResponse>($"payments/card/{requestId}/cancel", null, ct);
}
