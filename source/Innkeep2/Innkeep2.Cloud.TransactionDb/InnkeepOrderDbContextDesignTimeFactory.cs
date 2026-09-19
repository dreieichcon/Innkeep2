using Innkeep2.Cloud.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Innkeep2.Cloud.TransactionDb;

public sealed class InnkeepOrderDbContextDesignTimeFactory : IDesignTimeDbContextFactory<InnkeepOrderDbContext>
{
    public InnkeepOrderDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<InnkeepOrderDbContext>()
            .UseSqlite("Data Source=design-time.db")
            .Options;

        return new InnkeepOrderDbContext(options);
    }
}