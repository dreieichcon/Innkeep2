using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Innkeep2.Database.Tests.Repository;

public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
	public DbSet<TestEntity> TestEntities => Set<TestEntity>();
}

public sealed class TestDbContextFactory : IDbContextFactory<TestDbContext>, IDisposable
{
	private readonly SqliteConnection _connection;
	private readonly DbContextOptions<TestDbContext> _options;

	public TestDbContextFactory()
	{
		_connection = new SqliteConnection("DataSource=:memory:");
		_connection.Open();

		_options = new DbContextOptionsBuilder<TestDbContext>()
			.UseSqlite(_connection)
			.Options;

		using var context = new TestDbContext(_options);
		context.Database.EnsureCreated();
	}

	public TestDbContext CreateDbContext() => new(_options);

	public void Dispose() => _connection.Dispose();
}