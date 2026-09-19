namespace Innkeep2.Models.Internal.Receipt;

public sealed record ReceiptLine
{
    public required string Name { get; init; }
    public required decimal UnitPrice { get; init; }
    public required int Quantity { get; init; }

    public decimal TotalPrice => UnitPrice * Quantity;

    public static ReceiptLine FromSalesItem(SalesItem item)
        => new() { Name = item.Name, UnitPrice = item.Price, Quantity = item.Quantity ?? 1 };
}