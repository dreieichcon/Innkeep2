using System.Text.Json;
using Innkeep2.Cloud.TransactionDb.Models;
using Innkeep2.Models.Fiskaly.Transaction;
using Innkeep2.Models.Internal;
using Innkeep2.Models.Internal.Receipt;
using Innkeep2.Models.Pretix.Order;
using Innkeep2.Services.Shared;

namespace Innkeep2.Cloud.TransactionDb.Repositories;

public static class TransactionReceiptFactory
{
    public static TransactionReceipt FromTransaction(Transaction transaction)
    {
        var pretixOrder = transaction.PretixOrderJson is not null
            ? JsonSerializer.Deserialize<PretixOrderResponse>(transaction.PretixOrderJson)
            : null;

        var fiskalyTransaction = transaction.FiskalyTransactionJson is not null
            ? JsonSerializer.Deserialize<FiskalyTransaction>(transaction.FiskalyTransactionJson)
            : null;

        if (transaction.TransactionType == TransactionType.Transfer)
            return new TransactionReceipt
            {
                OrderId = transaction.RequestId,
                Title = transaction.Title,
                Header = transaction.Header,
                BookingTime = transaction.BookingTime,
                Currency = transaction.Currency ?? "",
                Lines = [],
                Sum = new ReceiptSum
                {
                    TotalAmount = transaction.TotalAmount,
                    AmountGiven = transaction.AmountGiven,
                    AmountReturned = transaction.AmountBack
                },
                TaxInformation = [],
                Vouchers = [],
                PretixOrderCode = null,
                FiskalyQrCode = fiskalyTransaction?.QrCodeData ?? "TSS OFFLINE",
                FiskalyTransactionNumber = fiskalyTransaction?.Number
            };

        var request = transaction.RequestJson is not null
            ? JsonSerializer.Deserialize<OrderRequest>(transaction.RequestJson)
            : null;

        return ReceiptBuilder.Build(
            transaction.RequestId,
            transaction.BookingTime,
            transaction.Title,
            transaction.Header,
            request!,
            transaction.AmountGiven,
            transaction.AmountBack,
            pretixOrder,
            fiskalyTransaction
        );
    }
}