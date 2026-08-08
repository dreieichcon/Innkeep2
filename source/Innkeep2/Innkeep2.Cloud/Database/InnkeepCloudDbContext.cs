using Innkeep2.Cloud.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Innkeep2.Cloud.Database;

public class InnkeepCloudDbContext(DbContextOptions<InnkeepCloudDbContext> options) : DbContext(options)
{
	public DbSet<InnkeepCloudSettings> InnkeepCloudSettings { get; set; } = null!;
}