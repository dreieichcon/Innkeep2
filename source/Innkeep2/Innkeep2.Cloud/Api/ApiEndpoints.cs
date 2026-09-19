using Innkeep2.Models.Internal;
using Innkeep2.Services.Cloud;
using Innkeep2.Services.Cloud.Cache;
using TransactionService = Innkeep2.Cloud.Services.Transactions.TransactionService;

namespace Innkeep2.Cloud.Api;

public static class ApiEndpoints
{
    public static void MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/auth", () => Results.Ok());

        app.MapPost("/transaction/create", async (OrderRequest request, TransactionService transactionService, CancellationToken ct) =>
        {
            var result = await transactionService.CreateOrderAsync(request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });
        
        app.MapPost("/transactions/{requestId:guid}/refund", async (Guid requestId, TransactionService transactionService, CancellationToken ct) =>
        {
            var result = await transactionService.RefundTransactionAsync(requestId, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });
        
        app.MapPost("/transactions/transfer", async (TransferRequest request, TransactionService transactionService, CancellationToken ct) =>
        {
            var result = await transactionService.CreateTransferAsync(request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        app.MapGet("/data/event", async (IActiveConfigurationService activeConfiguration, CancellationToken ct) =>
        {
            var activeEvent = activeConfiguration.Event;
            return activeEvent != null ? Results.Ok(activeEvent) : Results.InternalServerError("No Active Event");
        });
        
        app.MapGet("/data/salesitems", async (CachedSalesItemProvider salesItemProvider, IActiveConfigurationService activeConfig, CancellationToken ct) =>
        {
            if (activeConfig.Organizer is not { } organizer || activeConfig.Event is not { } pretixEvent)
                return Results.BadRequest("No organizer or event selected.");

            var result = await salesItemProvider.GetCachedItemsAsync(new SalesItemKey(organizer.Slug, pretixEvent.Slug), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });
    }
}