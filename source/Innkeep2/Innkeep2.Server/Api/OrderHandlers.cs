using Innkeep2.Models.Internal;
using Innkeep2.Server.Services;


namespace Innkeep2.Server.Api;

public static class OrderHandlers
{
    public static async Task<IResult> CreateOrderAsync(
        OrderRequest request,
        ServerTransactionService transactionService,
        CancellationToken ct
    )
    {
        var outcome = await transactionService.CreateOrderAsync(request, ct);
        return Results.Ok(outcome.Receipt);
    }
}