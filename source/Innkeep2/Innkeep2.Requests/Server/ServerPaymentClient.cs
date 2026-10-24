using System.Text.Json;
using Demolite.Core.Models;
using Demolite.Http.Client;
using Innkeep2.Models.Internal;
using Microsoft.Extensions.Logging;

namespace Innkeep2.Requests.Server;

public sealed class ServerPaymentClient(HttpClient httpClient, ILogger<ServerPaymentClient> logger)
    : ApiClient(httpClient, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, logger), IServerPaymentClient
{
    /// <summary>
    /// Starts a card payment through the Server.
    /// </summary>
    public Task<Result<PaymentStatusResponse>> StartCardPaymentAsync(OrderRequest request, CancellationToken ct = default)
        => PostAsync<PaymentStatusResponse>("payments/card/start", request, ct);

    /// <summary>
    /// Loads the current status of a card payment.
    /// </summary>
    public Task<Result<PaymentStatusResponse>> GetCardPaymentAsync(Guid requestId, CancellationToken ct = default)
        => GetAsync<PaymentStatusResponse>($"payments/card/{requestId}", ct);

    /// <summary>
    /// Asks the Server to cancel a pending card payment.
    /// </summary>
    public Task<Result<PaymentStatusResponse>> CancelCardPaymentAsync(Guid requestId, CancellationToken ct = default)
        => PostAsync<PaymentStatusResponse>($"payments/card/{requestId}/cancel", null, ct);
}
