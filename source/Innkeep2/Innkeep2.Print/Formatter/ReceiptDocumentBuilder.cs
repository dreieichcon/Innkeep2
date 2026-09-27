using ESCPOS;
using Innkeep2.Models.Internal.Receipt;
using Innkeep2.Print.Printer;

namespace Innkeep2.Print.Formatter;

public static class ReceiptDocumentBuilder
{
    public static byte[] Build(TransactionReceipt receipt)
    {
        var builder = new PrintDocumentBuilder();

        builder.AddTitle(receipt.Title);

        foreach (var line in receipt.Header.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
            builder.AddLine(line, Justification.Center);

        builder.AddLine(new string('-', 42));

        builder.AddLine(ReceiptFormatter.FormatReceiptTypeLabel(receipt));

        foreach (var line in ReceiptFormatter.FormatSubheading(receipt))
            builder.AddLine(line, Justification.Center);

        builder.AddLine(new string('-', 42));

        foreach (var line in receipt.Lines)
        foreach (var printLine in ReceiptFormatter.FormatLines(line))
            builder.AddLine(printLine);

        builder.AddLine(new string('-', 42));

        foreach (var line in ReceiptFormatter.FormatSum(receipt))
            builder.AddLine(line, Justification.Center);

        builder.AddLine(new string('-', 42));

        foreach (var line in ReceiptFormatter.FormatTaxInformation(receipt.TaxInformation))
            builder.AddLine(line);

        builder.AddLine(new string('-', 42));

        if (receipt.HasFiskalyQrCode)
            builder.AddQrCode(receipt.FiskalyQrCode!);

        builder.Cut();

        return builder.GetBytes();
    }
}