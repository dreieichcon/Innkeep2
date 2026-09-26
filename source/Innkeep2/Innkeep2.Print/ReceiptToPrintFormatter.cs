using Innkeep2.Models.Internal.Receipt;

namespace Innkeep2.Print;

public static class ReceiptFormatter
{
    private const int MaxLineWidth = 42;
    
    public static string[] FormatLines(ReceiptLine line)
    {
        var nameLines = WrapText($"{line.Quantity}x {line.Name}", MaxLineWidth);

        var leftPart = $"je {line.UnitPrice:N2} €";
        var rightPart = $"{line.TotalPrice:N2} € {line.TaxClass}";

        var spacing = MaxLineWidth - leftPart.Length - rightPart.Length;
        var detailLine = spacing > 0
            ? leftPart + new string(' ', spacing) + rightPart
            : leftPart + " " + rightPart;

        return [.. nameLines, detailLine];
    }
    
    public static string[] FormatTaxInformation(IReadOnlyList<ReceiptTaxInformation> taxInformation)
    {
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