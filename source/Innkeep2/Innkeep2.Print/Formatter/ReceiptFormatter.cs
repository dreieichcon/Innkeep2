using Innkeep2.Models.Internal.Receipt;
using Innkeep2.Models.Shared;

namespace Innkeep2.Print.Formatter;

public static class ReceiptFormatter
{
    private const int MaxLineWidth = 42;
    
    public static string[] FormatLines(ReceiptLine line)
    {
        var nameLines = WrapText($"{line.Quantity}x {line.Name}", MaxLineWidth);

        var leftPart = $"je {line.UnitPrice:N2} €";
        var rightPart = $"{line.TotalPrice:N2} € {line.TaxClass}";

        var detailLine = SpaceBetween(leftPart, rightPart);

        return [.. nameLines, detailLine];
    }
    
    public static string[] FormatTaxInformation(IReadOnlyList<ReceiptTaxInformation> taxInformation)
    {

        if (!taxInformation.Any())
            return [];
        
        const int classWidth = 3;
        const int rateWidth = 5;
        const int netWidth = 10;
        const int taxWidth = 10;
        const int grossWidth = 14;

        var lines = new List<string>
        {
            "St".PadRight(classWidth) +
            "%".PadRight(rateWidth) +
            "Netto".PadLeft(netWidth) +
            "Steuer".PadLeft(taxWidth) +
            "Brutto".PadLeft(grossWidth)
        };

        foreach (var tax in taxInformation)
        {
            lines.Add(
                tax.TaxClass.ToString().PadRight(classWidth) +
                $"{tax.TaxRate:0}%".PadRight(rateWidth) +
                $"{tax.Net:N2}".PadLeft(netWidth) +
                $"{tax.TaxAmount:N2}".PadLeft(taxWidth) +
                $"{tax.Gross:N2}".PadLeft(grossWidth)
            );
        }

        return lines.ToArray();
    }
    
    public static string FormatReceiptTypeLabel(TransactionReceipt receipt) => (receipt.TransactionType, receipt.IsCopy) switch
    {
        (TransactionType.Sale, false) => "Beleg",
        (TransactionType.Sale, true) => "Belegkopie",
        (TransactionType.Transfer, false) => "Transferbeleg",
        (TransactionType.Transfer, true) => "Transferbeleg (Kopie)",
        (TransactionType.Refund, false) => "Stornobeleg",
        (TransactionType.Refund, true) => "Stornobeleg (Kopie)",
        _ => "Beleg"
    };
    
    public static string[] FormatSubheading(TransactionReceipt receipt)
    {
        var lines = new List<string>
        {
            SpaceBetween("Datum:", receipt.BookingTime.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss")),
            SpaceBetween("Id:", receipt.OrderId.ToString())
        };

        if (receipt.TransactionType is TransactionType.Sale && receipt.PretixOrderCode is { } code)
            lines.Add(SpaceBetween("Pretix:", code));

        if (receipt.RefundRequestId is { } referenceId)
            lines.Add(SpaceBetween("Ref:", referenceId.ToString()));

        return lines.ToArray();
    }

    public static string[] FormatSum(TransactionReceipt receipt)
    {
        var paymentTypeLabel = receipt.PaymentType switch
        {
            PaymentType.Cash => "Bar",
            PaymentType.NonCash => "Karte",
            _ => ""
        };

        var sign = receipt.TransactionType == TransactionType.Refund ? "-" : "";

        return
        [
            SpaceBetween("Total", $"{sign}{receipt.Sum.TotalAmount:N2} €"),
            SpaceBetween($"Gegeben ({paymentTypeLabel})", $"{receipt.Sum.AmountGiven:N2} €"),
            SpaceBetween("Zurück", $"{receipt.Sum.AmountReturned:N2} €")
        ];
    }
    
    private static string SpaceBetween(string left, string right)
    {
        var spacing = MaxLineWidth - left.Length - right.Length;
        return spacing > 0
            ? left + new string(' ', spacing) + right
            : left + " " + right;
    }
    
    private static List<string> WrapText(string text, int maxLength)
    {
        var lines = new List<string>();
        var remaining = text;

        while (remaining.Length > maxLength)
        {
            var breakIndex = remaining.LastIndexOf(' ', maxLength);

            if (breakIndex <= 0)
                breakIndex = maxLength;

            lines.Add(remaining[..breakIndex].TrimEnd());
            remaining = remaining[breakIndex..].TrimStart();
        }

        lines.Add(remaining);

        return lines;
    }
}