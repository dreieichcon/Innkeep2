using Innkeep2.Models.Internal;
using Innkeep2.Requests.Cloud;
using Innkeep2.Server.Queue;
using Innkeep2.Server.Services;
using Innkeep2.Services.Cloud.Cache;
using Innkeep2.Services.Server;

namespace Innkeep2.Server.Api;

public static class ApiEndpoints
{
    public static void MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/auth", () => Results.Ok());
        
        app.MapGet("/data/event", async (ServerEventProvider eventProvider, CancellationToken ct) =>
        {
            var result = await eventProvider.GetCachedEventAsync(ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        app.MapGet("/data/salesitems", async (ServerSalesItemProvider salesItemProvider, CancellationToken ct) =>
        {
            var result = await salesItemProvider.GetCachedItemsAsync(ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });
        
        app.MapPost("/orders/create", async (
            OrderRequest request,
            ServerTransactionService transactionService,
            CancellationToken ct
        ) => await OrderHandlers.CreateOrderAsync(request, transactionService, ct));
    }
}