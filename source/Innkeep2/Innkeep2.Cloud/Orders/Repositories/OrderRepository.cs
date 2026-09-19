using Innkeep2.Cloud.Orders.Models;
using Innkeep2.Database.Repository;
using Microsoft.EntityFrameworkCore;

namespace Innkeep2.Cloud.Orders.Repositories;

public sealed class OrderRepository(IDbContextFactory<InnkeepOrderDbContext> contextFactory)
    : AbstractRepository<Order, InnkeepOrderDbContext>(contextFactory);