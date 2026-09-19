using Innkeep2.Cloud.TransactionDb.Models;
using Innkeep2.Cloud.TransactionDb.Repositories;
using Innkeep2.Models.Core;

namespace Innkeep2.Cloud.Tests.Integration.Orders;

internal sealed class FakeOrderRepository()
    : OrderRepository(new FakeOrderDbContextFactory())
{
    private readonly List<Order> _orders = [];
    private int _nextId = 1;

    public override Task<Result<Order>> CreateAsync(Order entity, CancellationToken ct = default)
    {
        entity.Id = _nextId++;
        _orders.Add(entity);
        return Task.FromResult(Result<Order>.Success(entity));
    }

    public override Task<Result<Order>> UpdateAsync(Order entity, CancellationToken ct = default)
        => Task.FromResult(Result<Order>.Success(entity));
}