using Demolite.Core.Models;
using Demolite.Db.Repositories;
using Innkeep2.Cloud.TransactionDb.Models;
using Innkeep2.Models.Shared;
using Microsoft.EntityFrameworkCore;

namespace Innkeep2.Cloud.TransactionDb.Repositories;

public class PaymentRequestRepository(IDbContextFactory<InnkeepTransactionDbContext> contextFactory)
    : AbstractRepository<PaymentRequest, int, InnkeepTransactionDbContext>(contextFactory)
{
    /// <summary>
    /// Loads the payment request with the given request id.
    /// </summary>
    public virtual Task<Result<PaymentRequest>> GetByRequestIdAsync(Guid requestId, CancellationToken ct = default)
        => GetCustomAsync(x => x.RequestId == requestId, ct);

    /// <summary>
    /// Loads the payment request that belongs to the SumUp client transaction id.
    /// </summary>
    public virtual Task<Result<PaymentRequest>> GetByClientTransactionIdAsync(string clientTransactionId, CancellationToken ct = default)
        => GetCustomAsync(x => x.ClientTransactionId == clientTransactionId, ct);

    /// <summary>
    /// Checks for a request that is still pending and was created at or after the given time.
    /// </summary>
    public virtual async Task<Result<bool>> HasPendingAsync(DateTime createdNotBeforeUtc, CancellationToken ct = default)
    {
        await using var context = CreateContext();

        var exists = await GetSet(context)
            .AnyAsync(x => x.Status == PaymentRequestStatus.Pending && x.CreatedAt >= createdNotBeforeUtc, ct);

        return Result<bool>.Success(exists);
    }
}
