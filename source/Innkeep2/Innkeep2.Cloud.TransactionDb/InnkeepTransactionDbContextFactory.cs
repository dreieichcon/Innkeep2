using Innkeep2.Services.Cloud;
using Microsoft.EntityFrameworkCore;

namespace Innkeep2.Cloud.TransactionDb;

public sealed class InnkeepTransactionDbContextFactory(IActiveConfigurationService activeConfiguration)
    : IDbContextFactory<InnkeepTransactionDbContext>
{
    public InnkeepTransactionDbContext CreateDbContext()
    {
        if (activeConfiguration.OrderDatabasePath is not { } path)
            throw new InvalidOperationException("No order database is currently selected.");

        var options = new DbContextOptionsBuilder<InnkeepTransactionDbContext>()
            .UseSqlite($"Data Source={path}")
            .Options;

        return new InnkeepTransactionDbContext(options);
    }
}