using System.Text.Json;
using Innkeep2.Models.Core;
using Innkeep2.Models.Internal;
using Innkeep2.Models.Internal.Receipt;
using Innkeep2.Requests.Cloud;
using Innkeep2.Requests.Core;
using Innkeep2.Server.Services;
using Innkeep2.Services.Server;
using Serilog;

namespace Innkeep2.Server.Queue;

public sealed class RequestRetryService(
   RequestQueueRepository queue,
   CloudTransactionClient cloudClient,
   ServerSalesItemProvider salesItemProvider
)
{
   private readonly SemaphoreSlim _lock = new(1, 1);

   public async Task RetryAllAsync(CancellationToken ct = default)
   {
      foreach (var entry in queue.GetAll())
         await RetryAsync(entry.Id, ct);
   }

   public async Task<Result<Unit>> RetryAsync(Guid id, CancellationToken ct = default)
   {
      await _lock.WaitAsync(ct);

      try
      {
         var entry = queue.Get(id);

         if (entry is null)
            return Result<Unit>.Failure(new Error("Queue.NotFound", $"Queued request '{id}' not found."));

         var result = await SendAsync(entry, ct);

         if (result.IsSuccess || IsAlreadyRefunded(entry, result))
         {
            queue.Remove(entry.Id);
            Log.Information("Retried {Type} request {RequestId} successfully", entry.Type, entry.RequestId);

            if (entry.Type is QueuedRequestType.Order or QueuedRequestType.Refund)
               _ = salesItemProvider.ForceRefreshAsync(CancellationToken.None);

            return Result<Unit>.Success(default);
         }

         entry.Attempts++;
         entry.LastAttemptAt = DateTime.UtcNow;
         entry.LastError = result.Error!.Message;
         queue.Update(entry);

         return Result<Unit>.Failure(result.Error!);
      }
      finally
      {
         _lock.Release();
      }
   }

   private async Task<Result<Unit>> SendAsync(QueuedRequest entry, CancellationToken ct)
   {
      var result = entry.Type switch
      {
         QueuedRequestType.Order => await cloudClient.CreateOrderAsync(Deserialize<OrderRequest>(entry), ct, isRetry: true),
         QueuedRequestType.Refund => await SendRefundAsync(entry, ct),
         QueuedRequestType.Transfer => await cloudClient.TransferAsync(Deserialize<TransferRequest>(entry), ct, isRetry: true),
         _ => throw new ArgumentOutOfRangeException(nameof(entry), entry.Type, "Unknown queued request type.")
      };

      return result.IsSuccess
         ? Result<Unit>.Success(default)
         : Result<Unit>.Failure(result.Error!);
   }

   private Task<Result<TransactionReceipt>> SendRefundAsync(QueuedRequest entry, CancellationToken ct)
   {
      var payload = Deserialize<RefundPayload>(entry);
      return cloudClient.RefundAsync(payload.OriginalRequestId, payload.RefundRequestId, ct, isRetry: true);
   }

   private static T Deserialize<T>(QueuedRequest entry)
      => JsonSerializer.Deserialize<T>(entry.PayloadJson)!;
   
   private static bool IsAlreadyRefunded(QueuedRequest entry, Result<Unit> result)
      => entry.Type == QueuedRequestType.Refund && result.Error?.Code == "Http.409";
}