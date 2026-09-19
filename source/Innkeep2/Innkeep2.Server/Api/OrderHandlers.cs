using System.Text.Json;
using Innkeep2.Models.Internal;
using Innkeep2.Models.Internal.Receipt;
using Innkeep2.Requests.Cloud;
using Innkeep2.Server.Queue;
using Innkeep2.Services.Server;
using Innkeep2.Services.Shared;

namespace Innkeep2.Server.Api;

public static class OrderHandlers
{
    public static async Task<IResult> CreateOrderAsync(
        OrderRequest request,
        CloudTransactionClient cloudClient,
        ServerEventProvider eventProvider,
        RequestQueueRepository queue,
        CancellationToken ct
    )
    {
        var result = await cloudClient.CreateOrderAsync(request, ct);

        if (result.IsSuccess)
            return Results.Ok(result.Value);

        EnqueuePending(queue, request.RequestId, QueuedRequestType.Order, request);

        var receipt = await BuildOfflineReceiptAsync(request, eventProvider, ct);

        return Results.Ok(receipt);
    }

    private static void EnqueuePending(RequestQueueRepository queue, Guid requestId, QueuedRequestType type, object payload)
        => queue.Enqueue(new QueuedRequest
        {
            RequestId = requestId,
            Type = type,
            PayloadJson = JsonSerializer.Serialize(payload),
            EnqueuedAt = DateTime.UtcNow
        });

    private static async Task<TransactionReceipt> BuildOfflineReceiptAsync(
        OrderRequest request,
        ServerEventProvider eventProvider,
        CancellationToken ct
    )
    {
        var eventResult = await eventProvider.GetCachedEventAsync(ct);
        var pretixEvent = eventResult.Value ?? new Event { Name = "", Slug = "", IsTestMode = false };

        return ReceiptBuilder.Build(
            request.RequestId,
            DateTime.UtcNow,
            pretixEvent,
            request,
            request.AmountGiven,
            request.AmountBack
        );
    }
}