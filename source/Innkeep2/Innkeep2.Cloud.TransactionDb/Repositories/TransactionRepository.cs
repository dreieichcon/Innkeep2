using Innkeep2.Cloud.TransactionDb.Models;
using Innkeep2.Database.Repository;
using Microsoft.EntityFrameworkCore;

namespace Innkeep2.Cloud.TransactionDb.Repositories;

public class TransactionRepository(IDbContextFactory<InnkeepTransactionDbContext> contextFactory)
    : AbstractRepository<Transaction, InnkeepTransactionDbContext>(contextFactory);