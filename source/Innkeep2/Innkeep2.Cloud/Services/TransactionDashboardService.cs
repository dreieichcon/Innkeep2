using Innkeep2.Cloud.TransactionDb;
using Innkeep2.Models.Shared;
using Microsoft.EntityFrameworkCore;

namespace Innkeep2.Cloud.Services;

public class TransactionDashboardService(IDbContextFactory<InnkeepTransactionDbContext> orderFactory)
{
    public async Task<int> TotalOrderCount()
    {
        await using var context = await orderFactory.CreateDbContextAsync();
        return await context.Transactions.CountAsync(x => x.TransactionType == TransactionType.Sale);
    }

    public async Task<decimal> TotalIncome()
    {
        await using var context = await orderFactory.CreateDbContextAsync();
        return await context.Transactions
            .Where(x => x.TransactionType == TransactionType.Sale)
            .SumAsync(x => x.TotalAmount);
    }

    public async Task<decimal> TotalCashIncome()
    {
        await using var context = await orderFactory.CreateDbContextAsync();
        return await context.Transactions
            .Where(x => x.TransactionType == TransactionType.Sale && x.PaymentType == PaymentType.Cash)
            .SumAsync(x => x.TotalAmount);
    }

    public async Task<decimal> CurrentRegisterBalance()
    {
        await using var context = await orderFactory.CreateDbContextAsync();
        return await context.Transactions
            .Where(x => x.PaymentType == PaymentType.Cash)
            .SumAsync(x => x.TotalAmount);
    }
}