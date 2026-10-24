using Innkeep2.Cloud.TransactionDb;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Innkeep2.Cloud.Tests.Integration.Payments;

internal sealed class SqliteTransactionDbContextFactory : IDbContextFactory<InnkeepTransactionDbContext>, IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public SqliteTransactionDbContextFactory()
    {
        _connection.Open();

        using var context = CreateDbContext();
        context.Database.Migrate();
    }

    public InnkeepTransactionDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<InnkeepTransactionDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new InnkeepTransactionDbContext(options);
    }

    public void Dispose()
        => _connection.Dispose();
}
