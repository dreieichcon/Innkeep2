using Innkeep2.Models.Internal.Receipt;
using Innkeep2.Models.Shared;
using Innkeep2.Print.Formatter;

namespace Innkeep2.Printer.Tests;

[TestClass]
public class ReceiptFormatterSumUpTests
{
    private const int LineWidth = 42;

    private static TransactionReceipt BuildReceipt(
        string? sumupCode,
        PaymentType paymentType = PaymentType.NonCash,
        TransactionType transactionType = TransactionType.Sale,
        long? tssNumber = 123,
        Guid? referenceId = null) => new()
    {
        OrderId = Guid.NewGuid(),
        TransactionType = transactionType,
        Title = "Title",
        Header = "Header",
        BookingTime = new DateTime(2026, 10, 24, 12, 0, 0, DateTimeKind.Utc),
        Currency = "EUR",
        Lines = [],
        Sum = new ReceiptSum { TotalAmount = 10m, AmountGiven = 10m, AmountReturned = 0m },
        TaxInformation = [],
        Vouchers = [],
        PaymentType = paymentType,
        PretixOrderCode = "ABC12",
        FiskalyTransactionNumber = tssNumber,
        RefundRequestId = referenceId,
        SumupTransactionCode = sumupCode
    };

    private static string[] Subheading(TransactionReceipt receipt)
        => ReceiptFormatter.FormatSubheading(receipt);

    [TestMethod]
    public void Subheading_ShowsTheSumUpNumberRightBelowTheTssCounter()
    {
        var lines = Subheading(BuildReceipt("TEENSK4W2K"));

        var tssIndex = Array.FindIndex(lines, x => x.StartsWith("TSS Zähler:"));
        Assert.IsGreaterThanOrEqualTo(0, tssIndex);
        Assert.StartsWith("SumUp Nr.:", lines[tssIndex + 1]);
        Assert.EndsWith("TEENSK4W2K", lines[tssIndex + 1]);
    }

    [TestMethod]
    public void Subheading_ShowsTheSumUpNumberBelowTssEvenWhenTheTssIsOffline()
    {
        var lines = Subheading(BuildReceipt("TEENSK4W2K", tssNumber: null));

        var tssIndex = Array.FindIndex(lines, x => x.StartsWith("TSS Zähler:"));
        Assert.StartsWith("SumUp Nr.:", lines[tssIndex + 1]);
    }

    [TestMethod]
    public void Subheading_FillsTheLineWidthWithTheNumberRightAligned()
    {
        var line = Subheading(BuildReceipt("TEENSK4W2K")).Single(x => x.StartsWith("SumUp Nr.:"));

        Assert.AreEqual(LineWidth, line.Length);
        Assert.EndsWith("TEENSK4W2K", line);
    }

    [TestMethod]
    public void Subheading_PutsTheSumUpNumberBeforeTheReferenceLine()
    {
        var lines = Subheading(BuildReceipt("TEENSK4W2K", transactionType: TransactionType.Refund, referenceId: Guid.NewGuid()));

        var sumUpIndex = Array.FindIndex(lines, x => x.StartsWith("SumUp Nr.:"));
        var referenceIndex = Array.FindIndex(lines, x => x.StartsWith("Ref:"));
        Assert.IsGreaterThanOrEqualTo(0, sumUpIndex);
        Assert.IsLessThan(referenceIndex, sumUpIndex);
    }

    [TestMethod]
    public void Subheading_ShowsTheSumUpNumberOnARefundReceipt()
    {
        var lines = Subheading(BuildReceipt("TEENSK4W2K", transactionType: TransactionType.Refund, referenceId: Guid.NewGuid()));

        Assert.IsTrue(lines.Any(x => x.StartsWith("SumUp Nr.:")));
    }

    [TestMethod]
    public void Subheading_ShowsTheSumUpNumberOnACopy()
    {
        var receipt = BuildReceipt("TEENSK4W2K");
        receipt.IsCopy = true;

        Assert.IsTrue(Subheading(receipt).Any(x => x.StartsWith("SumUp Nr.:")));
    }

    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    [TestMethod]
    public void Subheading_HasNoSumUpLineWithoutANumber(string? code)
    {
        var lines = Subheading(BuildReceipt(code));

        Assert.IsFalse(lines.Any(x => x.Contains("SumUp")));
    }

    [TestMethod]
    public void Subheading_HasNoSumUpLineOnACashReceipt()
    {
        var lines = Subheading(BuildReceipt(null, PaymentType.Cash));

        Assert.IsFalse(lines.Any(x => x.Contains("SumUp")));
    }

    [TestMethod]
    public void Subheading_KeepsTheOtherLinesUnchanged()
    {
        var receipt = BuildReceipt("TEENSK4W2K");

        var withCode = Subheading(receipt);
        var withoutCode = Subheading(receipt with { SumupTransactionCode = null });

        CollectionAssert.AreEqual(withoutCode, withCode.Where(x => !x.StartsWith("SumUp Nr.:")).ToArray());
        Assert.HasCount(withoutCode.Length + 1, withCode);
    }
}
