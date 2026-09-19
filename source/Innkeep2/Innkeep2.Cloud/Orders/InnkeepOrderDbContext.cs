using Innkeep2.Cloud.Orders.Models;
using Microsoft.EntityFrameworkCore;

namespace Innkeep2.Cloud.Orders;

public sealed class InnkeepOrderDbContext(DbContextOptions<InnkeepOrderDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
}