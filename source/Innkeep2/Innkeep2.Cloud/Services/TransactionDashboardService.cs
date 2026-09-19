using Innkeep2.Cloud.TransactionDb;
using Innkeep2.Models.Shared;
using Microsoft.EntityFrameworkCore;

namespace Innkeep2.Cloud.Services;

public class TransactionDashboardService(IDbContextFactory<InnkeepTransactionDbContext> orderFactory)
{
    
    public async Task<int> TotalOrderCount()
    {
        await using var orderDbContext = await orderFactory.CreateDbContextAsync();
        return await orderDbContext.Transactions.CountAsync();
    }
    
    public async Task<decimal> TotalCashSum()
    {
        await using var orderDbContext = await orderFactory.CreateDbContextAsync();
        return await orderDbContext.Transactions.Where(x => x.PaymentType == PaymentType.Cash).SumAsync(x => x.TotalAmount);
    }
}