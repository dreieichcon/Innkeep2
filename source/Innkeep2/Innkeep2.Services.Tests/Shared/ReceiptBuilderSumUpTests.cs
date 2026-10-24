using System.Text.Json;
using Innkeep2.Models.Internal;
using Innkeep2.Models.Internal.Receipt;
using Innkeep2.Models.Shared;
using Innkeep2.Services.Shared;

namespace Innkeep2.Services.Tests.Shared;

[TestClass]
public class ReceiptBuilderSumUpTests
{
    private static OrderRequest BuildOrder(PaymentType paymentType = PaymentType.NonCash) => new()
    {
        RequestId = Guid.NewGuid(),
        Items = [new SalesItem { Id = 1, VariationId = 0, Name = "Item", Price = 10m, TaxRate = 19m, Quantity = 1 }],
        PaymentType = paymentType,
        AmountGiven = 10m,
        Currency = "EUR"
    };

    private static ReceiptContext BuildContext(OrderRequest order, string? sumupCode = null, TransactionType type = TransactionType.Sale, Guid? reference = null)
        => new(order.RequestId, type, new DateTime(2026, 10, 24, 12, 0, 0, DateTimeKind.Utc), "Title", "Header", reference, sumupCode);

    [TestMethod]
    public void Build_CopiesTheSumUpNumberFromTheContext()
    {
        var order = BuildOrder();

        var receipt = ReceiptBuilder.Build(BuildContext(order, "TEENSK4W2K"), order, 10m, 0m);

        Assert.AreEqual("TEENSK4W2K", receipt.SumupTransactionCode);
    }

    [TestMethod]
    public void Build_HasNoSumUpNumberByDefault()
    {
        var order = BuildOrder(PaymentType.Cash);

        var receipt = ReceiptBuilder.Build(BuildContext(order), order, 10m, 0m);

        Assert.IsNull(receipt.SumupTransactionCode);
    }

    [TestMethod]
    public void Build_KeepsTheSumUpNumberOnARefundReceipt()
    {
        var order = BuildOrder();

        var receipt = ReceiptBuilder.Build(BuildContext(order, "TEENSK4W2K", TransactionType.Refund, Guid.NewGuid()), order, 0m, 10m);

        Assert.AreEqual("TEENSK4W2K", receipt.SumupTransactionCode);
        Assert.AreEqual(TransactionType.Refund, receipt.TransactionType);
    }

    [TestMethod]
    public void Build_DoesNotChangeTheOtherReceiptFields()
    {
        var order = BuildOrder();

        var withCode = ReceiptBuilder.Build(BuildContext(order, "TEENSK4W2K"), order, 10m, 0m);
        var withoutCode = ReceiptBuilder.Build(BuildContext(order), order, 10m, 0m);

        Assert.AreEqual(withoutCode.Sum, withCode.Sum);
        Assert.AreEqual(withoutCode.PretixOrderCode, withCode.PretixOrderCode);
        Assert.AreEqual(withoutCode.FiskalyQrCode, withCode.FiskalyQrCode);
        Assert.AreEqual(withoutCode.PaymentType, withCode.PaymentType);
    }

    [TestMethod]
    public void Receipt_TravelsAsJsonWithTheSumUpNumber()
    {
        var order = BuildOrder();
        var receipt = ReceiptBuilder.Build(BuildContext(order, "TEENSK4W2K"), order, 10m, 0m);

        var json = JsonSerializer.Serialize(receipt, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var read = JsonSerializer.Deserialize<TransactionReceipt>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.AreEqual("TEENSK4W2K", read!.SumupTransactionCode);
    }

    [TestMethod]
    public void Receipt_FromAnOlderServerWithoutTheFieldHasNoNumber()
    {
        var order = BuildOrder();
        var receipt = ReceiptBuilder.Build(BuildContext(order), order, 10m, 0m);
        var json = JsonSerializer.Serialize(receipt, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            .Replace(",\"sumupTransactionCode\":null", string.Empty);

        var read = JsonSerializer.Deserialize<TransactionReceipt>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.IsNotNull(read);
        Assert.IsNull(read.SumupTransactionCode);
    }
}
