using System.Linq.Expressions;
using Innkeep2.Cloud.TransactionDb.Models;
using Innkeep2.Cloud.TransactionDb.Repositories;
using Innkeep2.Models.Core;

namespace Innkeep2.Cloud.Tests.Integration.Orders;

internal sealed class FakeTransactionRepository()
    : TransactionRepository(new FakeOrderDbContextFactory())
{
    private readonly List<Transaction> _orders = [];
    private int _nextId = 1;

    public override Task<Result<Transaction>> CreateAsync(Transaction entity, CancellationToken ct = default)
    {
        entity.Id = _nextId++;
        _orders.Add(entity);
        return Task.FromResult(Result<Transaction>.Success(entity));
    }

    public override Task<Result<Transaction>> UpdateAsync(Transaction entity, CancellationToken ct = default)
        => Task.FromResult(Result<Transaction>.Success(entity));
    
    public override Task<Result<Transaction>> GetCustomAsync(
        Expression<Func<Transaction, bool>> predicate,
        CancellationToken ct = default
    )
    {
        var compiled = predicate.Compile();
        var entity = _orders.FirstOrDefault(compiled);

        return Task.FromResult(entity is not null
            ? Result<Transaction>.Success(entity)
            : Result<Transaction>.Failure(new Error("Db.NotFound", "Entity not found.")));
    }
}