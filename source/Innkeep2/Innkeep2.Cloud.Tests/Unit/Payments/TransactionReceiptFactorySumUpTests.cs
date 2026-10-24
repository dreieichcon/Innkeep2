using System.Text.Json;
using Innkeep2.Cloud.TransactionDb.Models;
using Innkeep2.Cloud.TransactionDb.Repositories;
using Innkeep2.Models.Internal;
using Innkeep2.Models.Shared;

namespace Innkeep2.Cloud.Tests.Unit.Payments;

[TestClass]
public class TransactionReceiptFactorySumUpTests
{
    private static Transaction BuildTransaction(string? sumupCode, PaymentType paymentType = PaymentType.NonCash, TransactionType type = TransactionType.Sale, Guid? refundOf = null)
    {
        var request = new OrderRequest
        {
            RequestId = Guid.NewGuid(),
            Items = [new SalesItem { Id = 1, VariationId = 0, Name = "Item", Price = 10m, TaxRate = 19m, Quantity = 1 }],
            PaymentType = paymentType,
            AmountGiven = 10m,
            Currency = "EUR"
        };

        return new Transaction
        {
            RequestId = request.RequestId,
            Title = "Title",
            Header = "Header",
            TransactionType = type,
            BookingTime = new DateTime(2026, 10, 24, 12, 0, 0, DateTimeKind.Utc),
            PaymentType = paymentType,
            TotalAmount = 10m,
            AmountGiven = 10m,
            AmountBack = 0m,
            Currency = "EUR",
            RequestJson = JsonSerializer.Serialize(request),
            RefundRequestId = refundOf,
            SumupTransactionCode = sumupCode
        };
    }

    [TestMethod]
    public void FromTransaction_ShowsTheSumUpNumberOfACardSale()
    {
        var receipt = TransactionReceiptFactory.FromTransaction(BuildTransaction("TEENSK4W2K"));

        Assert.AreEqual("TEENSK4W2K", receipt.SumupTransactionCode);
        Assert.AreEqual(PaymentType.NonCash, receipt.PaymentType);
    }

    [TestMethod]
    public void FromTransaction_HasNoSumUpNumberForACashSale()
    {
        var receipt = TransactionReceiptFactory.FromTransaction(BuildTransaction(null, PaymentType.Cash));

        Assert.IsNull(receipt.SumupTransactionCode);
    }

    [TestMethod]
    public void FromTransaction_HasNoSumUpNumberForACardSaleBookedBeforeTheFeature()
    {
        var receipt = TransactionReceiptFactory.FromTransaction(BuildTransaction(null));

        Assert.IsNull(receipt.SumupTransactionCode);
    }

    [TestMethod]
    public void FromTransaction_ShowsTheSumUpNumberOfTheRefundedPaymentOnARefund()
    {
        var receipt = TransactionReceiptFactory.FromTransaction(BuildTransaction("TEENSK4W2K", type: TransactionType.Refund, refundOf: Guid.NewGuid()));

        Assert.AreEqual("TEENSK4W2K", receipt.SumupTransactionCode);
        Assert.AreEqual(TransactionType.Refund, receipt.TransactionType);
    }

    [TestMethod]
    public void FromTransaction_NeverShowsASumUpNumberOnATransfer()
    {
        var transfer = BuildTransaction("TEENSK4W2K", PaymentType.Cash, TransactionType.Transfer);

        var receipt = TransactionReceiptFactory.FromTransaction(transfer);

        Assert.IsNull(receipt.SumupTransactionCode);
    }
}
