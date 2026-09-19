using System.Text.Json;
using Innkeep2.Cloud.TransactionDb.Models;
using Innkeep2.Models.Core;
using Innkeep2.Models.Fiskaly.Transaction;
using Innkeep2.Models.Internal;
using Innkeep2.Models.Internal.Receipt;
using Innkeep2.Models.Pretix.Order;
using Innkeep2.Services.Shared;
using Serilog;

namespace Innkeep2.Cloud.Services;

public sealed partial class TransactionService
{
    public async Task<Result<TransactionReceipt>> RefundTransactionAsync(Guid requestId, CancellationToken ct = default)
    {
        if (activeConfiguration.Organizer is not { } organizer || activeConfiguration.Event is not { } pretixEvent)
            return Result<TransactionReceipt>.Failure(
                new Error("Refund.NoConfiguration", "No organizer or event is currently selected."));

        var originalResult = await transactionRepository.GetCustomAsync(x => x.RequestId == requestId, ct);

        if (!originalResult.IsSuccess)
            return Result<TransactionReceipt>.Failure(originalResult.Error!);

        var original = originalResult.Value!;

        if (original.PretixStatus != TransactionStepStatus.Completed || original.PretixOrderJson is null)
            return Result<TransactionReceipt>.Failure(
                new Error("Refund.PretixOrderMissing",
                    "Original transaction has no completed Pretix order to refund."));

        var pretixOrder = JsonSerializer.Deserialize<PretixOrderResponse>(original.PretixOrderJson)!;
        var originalRequest = JsonSerializer.Deserialize<OrderRequest>(original.RequestJson!)!;

        var refund = new Transaction
        {
            RequestId = Guid.NewGuid(),
            TransactionType = TransactionType.Refund,
            RefundRequestId = original.RequestId,
            BookingTime = DateTime.UtcNow,
            PaymentType = original.PaymentType,
            TotalAmount = -original.TotalAmount,
            AmountGiven = 0,
            AmountBack = original.TotalAmount,
            Currency = original.Currency,
            RequestJson = original.RequestJson
        };

        var createResult = await transactionRepository.CreateAsync(refund, ct);

        if (!createResult.IsSuccess)
            return Result<TransactionReceipt>.Failure(createResult.Error!);

        refund = createResult.Value!;

        await TryRefundPretixOrderAsync(refund, organizer.Slug, pretixEvent.Slug, pretixOrder.Code,
            original.TotalAmount, ct);
        
        var fiskalyTransaction = await TryCreateFiskalyRefundAsync(refund, originalRequest, ct);

        await transactionRepository.UpdateAsync(refund, ct);

        var receipt = ReceiptBuilder.Build(
            refund.RequestId,
            refund.BookingTime,
            pretixEvent,
            originalRequest,
            refund.AmountGiven,
            refund.AmountBack,
            pretixOrder,
            fiskalyTransaction
        );

        return Result<TransactionReceipt>.Success(receipt);
    }

    private async Task TryRefundPretixOrderAsync(
        Transaction refund,
        string organizerSlug,
        string eventSlug,
        string orderCode,
        decimal amount,
        CancellationToken ct
    )
    {
        var createResult = await pretixOrderService.CreateRefundAsync(organizerSlug, eventSlug, orderCode, amount, ct);

        if (!createResult.IsSuccess)
        {
            refund.PretixStatus = TransactionStepStatus.Failed;
            Log.Warning("Pretix refund failed for transaction {RequestId}: {Error}", refund.RequestId,
                createResult.Error!.Message);
            return;
        }

        var doneResult =
            await pretixOrderService.MarkRefundDoneAsync(organizerSlug, eventSlug, orderCode,
                createResult.Value!.LocalId, ct);

        refund.PretixStatus = doneResult.IsSuccess ? TransactionStepStatus.Completed : TransactionStepStatus.Failed;
        refund.PretixOrderJson = JsonSerializer.Serialize(doneResult.Value ?? createResult.Value);
    }

    private async Task<FiskalyTransaction?> TryCreateFiskalyRefundAsync(
        Transaction refund,
        OrderRequest originalRequest,
        CancellationToken ct
    )
    {
        var startResult = await fiskalyTransactionService.StartAsync(refund.RequestId, ct);

        if (!startResult.IsSuccess)
        {
            refund.FiskalyStatus = TransactionStepStatus.Failed;
            Log.Warning("Fiskaly refund start failed for transaction {RequestId}: {Error}", refund.RequestId,
                startResult.Error!.Message);
            return null;
        }

        var finishResult =
            await fiskalyTransactionService.FinishAsync(refund.RequestId, revision: 2, originalRequest, sign: -1, ct);

        if (!finishResult.IsSuccess)
        {
            refund.FiskalyStatus = TransactionStepStatus.Failed;
            Log.Warning("Fiskaly refund finish failed for transaction {RequestId}: {Error}", refund.RequestId,
                finishResult.Error!.Message);
            return null;
        }

        refund.FiskalyStatus = TransactionStepStatus.Completed;
        refund.FiskalyTransactionJson = JsonSerializer.Serialize(finishResult.Value);

        return finishResult.Value;
    }
}