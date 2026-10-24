using Demolite.Core.Models;
using Innkeep2.Models.SumUp;

namespace Innkeep2.Requests.SumUp;

public interface ISumUpClient
{
    Task<Result<SumUpCheckoutResponse>> CreateCheckoutAsync(string merchantCode, string readerId, SumUpCheckoutRequest request, CancellationToken ct = default);

    Task<Result> TerminateAsync(string merchantCode, string readerId, CancellationToken ct = default);

    Task<Result<SumUpTransaction>> GetTransactionAsync(string merchantCode, string clientTransactionId, CancellationToken ct = default);
}
