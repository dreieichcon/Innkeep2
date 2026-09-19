namespace Innkeep2.Models.Internal.Receipt;

public sealed record ReceiptSum
{
    public required decimal TotalAmount { get; init; }
    public required decimal AmountGiven { get; init; }
    public required decimal AmountReturned { get; init; }
}