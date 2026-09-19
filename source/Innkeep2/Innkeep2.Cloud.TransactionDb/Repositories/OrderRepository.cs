using Innkeep2.Cloud.Orders;
using Innkeep2.Cloud.TransactionDb.Models;
using Innkeep2.Database.Repository;
using Microsoft.EntityFrameworkCore;

namespace Innkeep2.Cloud.TransactionDb.Repositories;

public class OrderRepository(IDbContextFactory<InnkeepOrderDbContext> contextFactory)
    : AbstractRepository<Order, InnkeepOrderDbContext>(contextFactory);