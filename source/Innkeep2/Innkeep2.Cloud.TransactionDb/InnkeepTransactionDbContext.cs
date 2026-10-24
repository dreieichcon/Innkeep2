using Innkeep2.Cloud.TransactionDb.Models;
using Microsoft.EntityFrameworkCore;

namespace Innkeep2.Cloud.TransactionDb;

public sealed class InnkeepTransactionDbContext(DbContextOptions<InnkeepTransactionDbContext> options) : DbContext(options)
{
    public DbSet<Transaction> Transactions => Set<Transaction>();

    public DbSet<PaymentRequest> PaymentRequests => Set<PaymentRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PaymentRequest>(entity =>
        {
            entity.HasIndex(x => x.RequestId).IsUnique();
            entity.HasIndex(x => x.ClientTransactionId);
        });
    }
}
