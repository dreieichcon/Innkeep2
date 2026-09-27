using System.Text.Json;
using Innkeep2.Cloud.TransactionDb.Models;
using Innkeep2.Models.Fiskaly.Transaction;
using Innkeep2.Models.Internal;
using Innkeep2.Models.Internal.Receipt;
using Innkeep2.Models.Pretix.Order;
using Innkeep2.Models.Shared;
using Innkeep2.Services.Shared;

namespace Innkeep2.Cloud.TransactionDb.Repositories;

public static class TransactionReceiptFactory
{
    public static TransactionReceipt FromTransaction(Transaction transaction)
    {
        var fiskalyTransaction = transaction.FiskalyTransactionJson is not null
            ? JsonSerializer.Deserialize<FiskalyTransaction>(transaction.FiskalyTransactionJson)
            : null;

        if (transaction.TransactionType == TransactionType.Transfer)
            return new TransactionReceipt
            {
                OrderId = transaction.RequestId,
                TransactionType = transaction.TransactionType,
                Title = transaction.Title,
                Header = transaction.Header,
                BookingTime = transaction.BookingTime,
                Currency = transaction.Currency ?? "",
                PaymentType = transaction.PaymentType,
                RefundRequestId = transaction.RefundRequestId,
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

        // Only Sale transactions store a PretixOrderResponse in PretixOrderJson.
        // Refund rows store a PretixRefund there instead, which has no order code of its own.
        var pretixOrder = transaction is { TransactionType: TransactionType.Sale, PretixOrderJson: not null }
            ? JsonSerializer.Deserialize<PretixOrderResponse>(transaction.PretixOrderJson)
            : null;

        return ReceiptBuilder.Build(
            new ReceiptContext(
                transaction.RequestId,
                transaction.TransactionType,
                transaction.BookingTime,
                transaction.Title,
                transaction.Header,
                transaction.RefundRequestId
            ),
            request!,
            transaction.AmountGiven,
            transaction.AmountBack,
            pretixOrder,
            fiskalyTransaction
        );
    }
}