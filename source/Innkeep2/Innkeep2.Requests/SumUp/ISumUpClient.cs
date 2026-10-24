using Demolite.Core.Models;
using Innkeep2.Models.SumUp;

namespace Innkeep2.Requests.SumUp;

public interface ISumUpClient
{
    Task<Result<SumUpCheckoutResponse>> CreateCheckoutAsync(string merchantCode, string readerId, SumUpCheckoutRequest request, CancellationToken ct = default);

    Task<Result> TerminateAsync(string merchantCode, string readerId, CancellationToken ct = default);

    Task<Result<SumUpTransaction>> GetTransactionAsync(string merchantCode, string clientTransactionId, CancellationToken ct = default);

    Task<Result<SumUpReader>> CreateReaderAsync(string merchantCode, SumUpCreateReaderRequest request, CancellationToken ct = default);

    Task<Result<SumUpReader>> GetReaderAsync(string merchantCode, string readerId, CancellationToken ct = default);

    Task<Result<SumUpReaderList>> ListReadersAsync(string merchantCode, CancellationToken ct = default);

    Task<Result> DeleteReaderAsync(string merchantCode, string readerId, CancellationToken ct = default);
}
