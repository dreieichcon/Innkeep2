namespace Innkeep2.Models.Internal.Receipt;

public sealed record ReceiptVoucher
{
    public required string ItemName { get; init; }
    public string? Secret { get; init; }
}