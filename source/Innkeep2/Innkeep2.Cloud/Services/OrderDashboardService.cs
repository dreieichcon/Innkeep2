using Innkeep2.Cloud.Orders;
using Innkeep2.Models.Shared;
using Microsoft.EntityFrameworkCore;

namespace Innkeep2.Cloud.Services;

public class OrderDashboardService(IDbContextFactory<InnkeepOrderDbContext> orderFactory)
{
    
    public async Task<int> TotalOrderCount()
    {
        await using var orderDbContext = await orderFactory.CreateDbContextAsync();
        return await orderDbContext.Orders.CountAsync();
    }
    
    public async Task<decimal> TotalCashSum()
    {
        await using var orderDbContext = await orderFactory.CreateDbContextAsync();
        return orderDbContext.Orders.Where(x => x.PaymentType == PaymentType.Cash).Sum(x => x.TotalAmount);
    }
}