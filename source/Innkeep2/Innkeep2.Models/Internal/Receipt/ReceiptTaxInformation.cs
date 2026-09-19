namespace Innkeep2.Models.Internal.Receipt;

public sealed record ReceiptTaxInformation
{
    public required decimal TaxRate { get; init; }
    public required decimal Net { get; init; }
    public required decimal TaxAmount { get; init; }
    public required decimal Gross { get; init; }
}