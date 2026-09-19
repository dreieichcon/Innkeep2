using Innkeep2.Cloud.Orders;
using Microsoft.EntityFrameworkCore;

namespace Innkeep2.Cloud.Tests.Integration.Orders;

internal sealed class FakeOrderDbContextFactory : IDbContextFactory<InnkeepOrderDbContext>
{
    public InnkeepOrderDbContext CreateDbContext()
        => throw new NotSupportedException("Fake factory, real context is never created.");
}