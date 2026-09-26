using System.Text.Json;
using Innkeep2.Cloud.TransactionDb.Models;
using Innkeep2.Models.Core;
using Innkeep2.Models.Fiskaly.Transaction;
using Innkeep2.Models.Internal;
using Innkeep2.Models.Internal.Receipt;
using Innkeep2.Models.Shared;
using Innkeep2.Services.Shared;
using Serilog;

namespace Innkeep2.Cloud.Services.Transactions;

public sealed partial class TransactionService
{
    public async Task<Result<TransactionReceipt>> CreateTransferAsync(TransferRequest request, CancellationToken ct = default)
    {
        if (activeConfiguration.Tss is null || activeConfiguration.Client is null || activeConfiguration.Event is not { } pretixEvent)
            return Result<TransactionReceipt>.Failure(
                new Error("Transfer.NoConfiguration", "No TSS, client, or event is currently selected."));

        var transfer = new Transaction
        {
            RequestId = request.RequestId,
            TransactionType = TransactionType.Transfer,
            Title = pretixEvent.Name,
            Header = pretixEvent.Header ?? "",
            BookingTime = DateTime.UtcNow,
            PaymentType = PaymentType.Cash,
            TotalAmount = request.Amount,
            AmountGiven = request.Amount >= 0 ? request.Amount : 0,
            AmountBack = request.Amount < 0 ? -request.Amount : 0,
            Currency = request.Currency,
            RequestJson = JsonSerializer.Serialize(request)
        };

        var createResult = await transactionRepository.CreateAsync(transfer, ct);

        if (!createResult.IsSuccess)
            return Result<TransactionReceipt>.Failure(createResult.Error!);

        transfer = createResult.Value!;

        var fiskalyTransaction = await TryCreateFiskalyTransferAsync(transfer, request.Amount, request.Currency, ct);

        await transactionRepository.UpdateAsync(transfer, ct);

        var receipt = ReceiptBuilder.BuildTransfer(
            transfer.RequestId,
            transfer.BookingTime,
            pretixEvent.Name,
            pretixEvent.Header ?? "",
            request.Amount,
            transfer.AmountGiven,
            transfer.AmountBack,
            request.Currency,
            fiskalyTransaction
        );

        return Result<TransactionReceipt>.Success(receipt);
    }

    private async Task<FiskalyTransaction?> TryCreateFiskalyTransferAsync(
        Transaction transfer,
        decimal amount,
        string currency,
        CancellationToken ct
    )
    {
        var startResult = await fiskalyTransactionService.StartAsync(transfer.RequestId, ct);

        if (!startResult.IsSuccess)
        {
            transfer.FiskalyStatus = TransactionStepStatus.Failed;
            Log.Warning("Fiskaly transfer start failed for transaction {RequestId}: {Error}", transfer.RequestId,
                startResult.Error!.Message);
            return null;
        }

        var finishResult =
            await fiskalyTransactionService.FinishTransferAsync(transfer.RequestId, revision: 2, amount, currency, ct);

        if (!finishResult.IsSuccess)
        {
            transfer.FiskalyStatus = TransactionStepStatus.Failed;
            Log.Warning("Fiskaly transfer finish failed for transaction {RequestId}: {Error}", transfer.RequestId,
                finishResult.Error!.Message);
            return null;
        }

        transfer.FiskalyStatus = TransactionStepStatus.Completed;
        transfer.FiskalyTransactionJson = JsonSerializer.Serialize(finishResult.Value);

        return finishResult.Value;
    }
}