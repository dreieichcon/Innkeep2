using Innkeep2.Cloud.TransactionDb.Models;
using Innkeep2.Database.Repository;
using Innkeep2.Models.Core;
using Microsoft.EntityFrameworkCore;

namespace Innkeep2.Cloud.TransactionDb.Repositories;

public class TransactionRepository(IDbContextFactory<InnkeepTransactionDbContext> contextFactory)
    : AbstractRepository<Transaction, InnkeepTransactionDbContext>(contextFactory)
{
    public virtual async Task<Result<PagedResult<Transaction>>> GetPagedAsync(
        int skip,
        int take,
        CancellationToken ct = default
    )
    {
        await using var context = CreateContext();

        var query = GetSet(context).OrderByDescending(x => x.BookingTime);

        var total = await query.CountAsync(ct);
        var items = await query.Skip(skip).Take(take).ToListAsync(ct);

        return Result<PagedResult<Transaction>>.Success(new PagedResult<Transaction>(items, total));
    }
}