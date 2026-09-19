using Innkeep2.Cloud.TransactionDb;
using Microsoft.EntityFrameworkCore;

namespace Innkeep2.Cloud.Tests.Integration.Orders;

internal sealed class FakeOrderDbContextFactory : IDbContextFactory<InnkeepTransactionDbContext>
{
    public InnkeepTransactionDbContext CreateDbContext()
        => throw new NotSupportedException("Fake factory, real context is never created.");
}