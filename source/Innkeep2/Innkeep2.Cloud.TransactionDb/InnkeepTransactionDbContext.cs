using Innkeep2.Cloud.TransactionDb.Models;
using Microsoft.EntityFrameworkCore;

namespace Innkeep2.Cloud.TransactionDb;

public sealed class InnkeepTransactionDbContext(DbContextOptions<InnkeepTransactionDbContext> options) : DbContext(options)
{
    public DbSet<Transaction> Transactions => Set<Transaction>();
}