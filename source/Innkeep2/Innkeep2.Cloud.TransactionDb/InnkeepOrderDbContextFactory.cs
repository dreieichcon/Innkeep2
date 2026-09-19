using Innkeep2.Cloud.Orders;
using Innkeep2.Services.Cloud;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;

namespace Innkeep2.Cloud.TransactionDb;

public sealed class InnkeepOrderDbContextFactory(IActiveConfigurationService activeConfiguration)
    : IDbContextFactory<InnkeepOrderDbContext>
{
    public InnkeepOrderDbContext CreateDbContext()
    {
        if (activeConfiguration.OrderDatabasePath is not { } path)
            throw new InvalidOperationException("No order database is currently selected.");

        var options = new DbContextOptionsBuilder<InnkeepOrderDbContext>()
            .UseSqlite($"Data Source={path}")
            .Options;

        return new InnkeepOrderDbContext(options);
    }
}