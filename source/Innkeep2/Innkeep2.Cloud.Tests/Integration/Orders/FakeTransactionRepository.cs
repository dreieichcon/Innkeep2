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
}