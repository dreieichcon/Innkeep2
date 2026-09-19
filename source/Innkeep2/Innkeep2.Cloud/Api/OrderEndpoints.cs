using Innkeep2.Cloud.Services;
using Innkeep2.Models.Internal;
using Innkeep2.Services.Cloud;
using Innkeep2.Services.Cloud.Cache;

namespace Innkeep2.Cloud.Api;

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/auth", () => Results.Ok());

        app.MapPost("/orders/create", async (OrderRequest request, TransactionService transactionService, CancellationToken ct) =>
        {
            var result = await transactionService.CreateOrderAsync(request, ct);
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