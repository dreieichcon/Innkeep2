using System.Text.Json;
using Demolite.Core.Models;
using Demolite.Http.Client;
using Innkeep2.Models.SumUp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Innkeep2.Requests.SumUp;

public sealed class SumUpClient(HttpClient httpClient, [FromKeyedServices("sumup")] JsonSerializerOptions serializerOptions, ILogger<SumUpClient> logger)
    : ApiClient(httpClient, serializerOptions, logger), ISumUpClient
{
    /// <summary>
    /// Starts a checkout on the reader. The reader must start the payment within 60 seconds.
    /// </summary>
    public Task<Result<SumUpCheckoutResponse>> CreateCheckoutAsync(
        string merchantCode,
        string readerId,
        SumUpCheckoutRequest request,
        CancellationToken ct = default
    )
        => PostAsync<SumUpCheckoutResponse>($"v0.1/merchants/{Escape(merchantCode)}/readers/{Escape(readerId)}/checkout", request, ct);

    /// <summary>
    /// Terminates the running checkout. Only works while the reader waits for the cardholder.
    /// </summary>
    public Task<Result> TerminateAsync(string merchantCode, string readerId, CancellationToken ct = default)
        => PostAsync($"v0.1/merchants/{Escape(merchantCode)}/readers/{Escape(readerId)}/terminate", null!, ct);

    /// <summary>
    /// Looks up the transaction of a checkout by its client transaction id.
    /// </summary>
    public Task<Result<SumUpTransaction>> GetTransactionAsync(string merchantCode, string clientTransactionId, CancellationToken ct = default)
        => GetAsync<SumUpTransaction>($"v2.1/merchants/{Escape(merchantCode)}/transactions?client_transaction_id={Escape(clientTransactionId)}", ct);

    /// <summary>
    /// Pairs a reader with the merchant. The reader starts as processing until the device confirms.
    /// </summary>
    public Task<Result<SumUpReader>> CreateReaderAsync(string merchantCode, SumUpCreateReaderRequest request, CancellationToken ct = default)
        => PostAsync<SumUpReader>($"v0.1/merchants/{Escape(merchantCode)}/readers", request, ct);

    /// <summary>
    /// Retrieves a reader, which includes its pairing status.
    /// </summary>
    public Task<Result<SumUpReader>> GetReaderAsync(string merchantCode, string readerId, CancellationToken ct = default)
        => GetAsync<SumUpReader>($"v0.1/merchants/{Escape(merchantCode)}/readers/{Escape(readerId)}", ct);

    /// <summary>
    /// Lists all readers of the merchant.
    /// </summary>
    public Task<Result<SumUpReaderList>> ListReadersAsync(string merchantCode, CancellationToken ct = default)
        => GetAsync<SumUpReaderList>($"v0.1/merchants/{Escape(merchantCode)}/readers", ct);

    /// <summary>
    /// Deletes the reader at SumUp. The device must also be disconnected on the device itself.
    /// </summary>
    public Task<Result> DeleteReaderAsync(string merchantCode, string readerId, CancellationToken ct = default)
        => DeleteAsync($"v0.1/merchants/{Escape(merchantCode)}/readers/{Escape(readerId)}", ct);

    private static string Escape(string value)
        => Uri.EscapeDataString(value);
}
