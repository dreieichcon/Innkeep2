using Demolite.Core.Models;
using Demolite.Db.Repositories;
using Innkeep2.Cloud.TransactionDb.Models;
using Innkeep2.Models.Core;
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
    /// <summary>
    /// Loads a page of payment requests, newest first, with the state of the order that belongs to each one.
    /// Optionally only the requests with a status, or only those that need attention.
    /// </summary>
    public virtual async Task<Result<PagedResult<PaymentOverviewRow>>> GetOverviewAsync(
        int skip,
        int take,
        PaymentRequestStatus? status,
        bool onlyNeedingAttention,
        DateTime hangingPendingCreatedBeforeUtc,
        CancellationToken ct = default
    )
    {
        await using var context = CreateContext();

        var query = GetSet(context).AsQueryable();

        if (status is { } wanted)
            query = query.Where(x => x.Status == wanted);

        if (onlyNeedingAttention)
        {
            query = query.Where(x =>
                (x.Status == PaymentRequestStatus.Pending && x.CreatedAt <= hangingPendingCreatedBeforeUtc)
                || x.Status == PaymentRequestStatus.Paid
                || (x.Status == PaymentRequestStatus.Completed
                    && !context.Transactions.Any(t => t.RequestId == x.RequestId && t.TransactionType == TransactionType.Sale)));
        }

        var total = await query.CountAsync(ct);

        var rows = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Select(x => new
            {
                Payment = x,
                Order = context.Transactions
                    .Where(t => t.RequestId == x.RequestId && t.TransactionType == TransactionType.Sale)
                    .Select(t => new { t.PretixStatus, t.FiskalyStatus })
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        var items = rows
            .Select(x => new PaymentOverviewRow(
                x.Payment,
                x.Order is not null,
                x.Order?.PretixStatus,
                x.Order?.FiskalyStatus))
            .ToList();

        return Result<PagedResult<PaymentOverviewRow>>.Success(new PagedResult<PaymentOverviewRow>(items, total));
    }
}
