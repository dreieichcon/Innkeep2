using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Innkeep2.Cloud.TransactionDb;

public sealed class InnkeepTransactionDbContextDesignTimeFactory : IDesignTimeDbContextFactory<InnkeepTransactionDbContext>
{
    public InnkeepTransactionDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<InnkeepTransactionDbContext>()
            .UseSqlite("Data Source=design-time.db")
            .Options;

        return new InnkeepTransactionDbContext(options);
    }
}