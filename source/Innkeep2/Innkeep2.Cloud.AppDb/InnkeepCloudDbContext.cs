using Innkeep2.Cloud.AppDb.Models;
using Microsoft.EntityFrameworkCore;

namespace Innkeep2.Cloud.AppDb;

public class InnkeepCloudDbContext(DbContextOptions<InnkeepCloudDbContext> options) : DbContext(options)
{
	public DbSet<InnkeepCloudSettings> InnkeepCloudSettings { get; set; } = null!;

	public DbSet<InnkeepCloudApiKey> InnkeepCloudApiKeys { get; set; } = null!;
}