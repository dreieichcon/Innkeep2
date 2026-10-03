using System.Text.Json;
using Innkeep2.Cloud.TransactionDb.Models;
using Innkeep2.Cloud.TransactionDb.Repositories;
using Innkeep2.Models.Core;
using Innkeep2.Models.Internal;
using Innkeep2.Models.Internal.Receipt;
using Innkeep2.Models.Pretix.Order;
using Innkeep2.Models.Shared;
using Innkeep2.Services.Cloud;
using Innkeep2.Services.Cloud.Cache;
using TransactionService = Innkeep2.Cloud.Services.Transactions.TransactionService;

namespace Innkeep2.Cloud.Api;

public static class ApiEndpoints
{
    public static void MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/auth", () => Results.Ok());

        app.MapTransactionEndpoints();

        app.MapGet("/data/event", async (IActiveConfigurationService activeConfiguration, CancellationToken ct) =>
        {
            var activeEvent = activeConfiguration.Event;
            return activeEvent != null ? Results.Ok(activeEvent) : Results.InternalServerError("No Active Event");
        });

        app.MapGet("/data/salesitems", async (CachedSalesItemProvider salesItemProvider,
            IActiveConfigurationService activeConfig, CancellationToken ct) =>
        {
            if (activeConfig.Organizer is not { } organizer || activeConfig.Event is not { } pretixEvent)
                return Results.BadRequest("No organizer or event selected.");

            var result =
                await salesItemProvider.GetCachedItemsAsync(new SalesItemKey(organizer.Slug, pretixEvent.Slug), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });
    }

    private static void MapTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/transactions", async (
            int skip,
            int take,
            TransactionRepository repository,
            CancellationToken ct
        ) =>
        {
            var result = await repository.GetPagedAsync(skip, take, ct);

            if (!result.IsSuccess)
                return Results.BadRequest(result.Error);

            var summaries = result.Value!.Items.Select(x => new TransactionSummary
            {
                RequestId = x.RequestId,
                PretixOrderCode = ExtractPretixOrderCode(x),
                RefundRequestId = x.RefundRequestId,
                BookingTime = x.BookingTime,
                TransactionType = x.TransactionType,
                TotalAmount = x.TotalAmount,
                PretixStatus = x.PretixStatus,
                FiskalyStatus = x.FiskalyStatus
            }).ToList();

            return Results.Ok(new PagedResult<TransactionSummary>(summaries, result.Value!.TotalCount));
        });

        app.MapGet("/transactions/{requestId:guid}", async (
            Guid requestId,
            TransactionRepository repository,
            CancellationToken ct
        ) =>
        {
            var result = await repository.GetCustomAsync(x => x.RequestId == requestId, ct);

            if (!result.IsSuccess)
                return Results.BadRequest(result.Error);

            var receipt = TransactionReceiptFactory.FromTransaction(result.Value!);

            var hasRefundResult = await repository.HasRefundAsync(requestId, ct);
            receipt = receipt with { IsRefunded = hasRefundResult.Value };

            return Results.Ok(receipt);
        });

        app.MapPost("/transactions/create",
            async (OrderRequest request, TransactionService transactionService, CancellationToken ct,
                bool retry = false) =>
            {
                var result = await transactionService.CreateOrderAsync(request, ct, retry);
                return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
            });

        app.MapPost("/transactions/{requestId:guid}/refund/{refundRequestId:guid}",
            async (Guid requestId, Guid refundRequestId, TransactionService transactionService, CancellationToken ct, bool retry = false) =>
            {
                var result = await transactionService.RefundTransactionAsync(requestId, refundRequestId, ct, retry);

                if (result.IsSuccess)
                    return Results.Ok(result.Value);

                return result.Error!.Code == "Refund.AlreadyRefunded"
                    ? Results.Conflict(result.Error)
                    : Results.BadRequest(result.Error);
            });

        app.MapPost("/transactions/transfer",
            async (TransferRequest request, TransactionService transactionService, CancellationToken ct,
                bool retry = false) =>
            {
                var result = await transactionService.CreateTransferAsync(request, ct, retry);
                return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
            });
    }

    private static string? ExtractPretixOrderCode(Transaction transaction)
    {
        if (transaction.PretixOrderJson is null || transaction.TransactionType != TransactionType.Sale)
            return null;

        try
        {
            var pretixOrder = JsonSerializer.Deserialize<PretixOrderResponse>(transaction.PretixOrderJson);
            return pretixOrder?.Code;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}