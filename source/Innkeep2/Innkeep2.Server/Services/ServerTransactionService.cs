using System.Text.Json;
using Innkeep2.Models.Internal;
using Innkeep2.Models.Internal.Receipt;
using Innkeep2.Models.Shared;
using Innkeep2.Requests.Cloud;
using Innkeep2.Server.Queue;
using Innkeep2.Services.Server;
using Innkeep2.Services.Shared;
using Serilog;

namespace Innkeep2.Server.Services;

public sealed record SubmitOutcome(TransactionReceipt? Receipt, bool WasQueued, string? Error = null);

public sealed record RefundPayload(Guid OriginalRequestId, Guid RefundRequestId);


public sealed class ServerTransactionService(
    CloudTransactionClient cloudClient,
    ServerEventProvider eventProvider,
    ServerSalesItemProvider salesItemProvider,
    RequestQueueRepository queue
)
{
    public async Task<SubmitOutcome> TransferAsync(TransferRequest request, CancellationToken ct = default)
    {
        var result = await cloudClient.TransferAsync(request, ct);

        if (result.IsSuccess)
            return new SubmitOutcome(result.Value, false);

        Log.Warning("Transfer {RequestId} failed, queueing: {Error}", request.RequestId, result.Error!.Message);
        Enqueue(request.RequestId, QueuedRequestType.Transfer, request);

        return new SubmitOutcome(BuildOfflineTransferReceipt(request), true);
    }

    public async Task<SubmitOutcome> RefundAsync(TransactionReceipt original, CancellationToken ct = default)
    {
        var queued = FindQueuedRefund(original.OrderId);
        var refundRequestId = queued is null
            ? Guid.NewGuid()
            : JsonSerializer.Deserialize<RefundPayload>(queued.PayloadJson)!.RefundRequestId;

        var result = await cloudClient.RefundAsync(original.OrderId, refundRequestId, ct, isRetry: queued is not null);

        if (result.IsSuccess)
        {
            if (queued is not null)
                queue.Remove(queued.Id);

            _ = salesItemProvider.ForceRefreshAsync(CancellationToken.None);
            return new SubmitOutcome(result.Value, false);
        }

        if (result.Error!.Code == "Http.409")
        {
            if (queued is not null)
                queue.Remove(queued.Id);

            return new SubmitOutcome(null, false, "Bestellung wurde bereits storniert.");
        }

        Log.Warning("Refund of {RequestId} failed, queueing: {Error}", original.OrderId, result.Error.Message);

        if (queued is null)
            Enqueue(original.OrderId, QueuedRequestType.Refund, new RefundPayload(original.OrderId, refundRequestId));

        var offlineReceipt = ReceiptBuilder.BuildOfflineRefund(
            new ReceiptContext(refundRequestId, TransactionType.Refund, DateTime.UtcNow, original.Title, original.Header, original.OrderId),
            original.Sum.TotalAmount,
            original.PaymentType,
            original.Currency
        );

        return new SubmitOutcome(offlineReceipt, true);
    }

    public async Task<SubmitOutcome> CreateOrderAsync(OrderRequest request, CancellationToken ct = default)
    {
        var result = await cloudClient.CreateOrderAsync(request, ct);

        if (result.IsSuccess)
        {
            _ = salesItemProvider.ForceRefreshAsync(CancellationToken.None);
            return new SubmitOutcome(result.Value, false);
        }

        Log.Warning("Order {RequestId} failed, queueing: {Error}", request.RequestId, result.Error!.Message);
        Enqueue(request.RequestId, QueuedRequestType.Order, request);

        return new SubmitOutcome(BuildOfflineOrderReceipt(request), true);
    }
    
    public QueuedRequest? FindQueued(Guid transactionId)
        => queue.GetAll().FirstOrDefault(x => x.RequestId == transactionId || IsQueuedRefundFor(x, transactionId));

    private static bool IsQueuedRefundFor(QueuedRequest entry, Guid transactionId)
        => entry.Type == QueuedRequestType.Refund
           && JsonSerializer.Deserialize<RefundPayload>(entry.PayloadJson)!.RefundRequestId == transactionId;

    private QueuedRequest? FindQueuedRefund(Guid originalRequestId)
        => queue.GetAll().FirstOrDefault(x => x.Type == QueuedRequestType.Refund && x.RequestId == originalRequestId);

    private Event CachedEventOrEmpty()
        => eventProvider.LastKnownEvent ?? new Event { Name = "", Slug = "", IsTestMode = false };

    private TransactionReceipt BuildOfflineOrderReceipt(OrderRequest request)
    {
        var pretixEvent = CachedEventOrEmpty();

        return ReceiptBuilder.Build(
            new ReceiptContext(request.RequestId, TransactionType.Sale, DateTime.UtcNow, pretixEvent.Name, pretixEvent.Header ?? ""),
            request,
            request.AmountGiven,
            request.AmountBack
        );
    }

    private void Enqueue(Guid requestId, QueuedRequestType type, object payload)
        => queue.Enqueue(new QueuedRequest
        {
            RequestId = requestId,
            Type = type,
            PayloadJson = JsonSerializer.Serialize(payload),
            EnqueuedAt = DateTime.UtcNow
        });

    private TransactionReceipt BuildOfflineTransferReceipt(TransferRequest request)
    {
        var pretixEvent = CachedEventOrEmpty();

        return ReceiptBuilder.BuildTransfer(
            new ReceiptContext(request.RequestId, TransactionType.Transfer, DateTime.UtcNow, pretixEvent.Name,
                pretixEvent.Header ?? ""),
            request.Amount,
            request.Amount >= 0 ? request.Amount : 0,
            request.Amount < 0 ? -request.Amount : 0,
            request.Currency,
            null
        );
    }
}