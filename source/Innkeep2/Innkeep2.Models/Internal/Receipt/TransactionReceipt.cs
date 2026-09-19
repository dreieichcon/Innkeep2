namespace Innkeep2.Models.Internal.Receipt;

public sealed record TransactionReceipt
{
    public required Guid OrderId { get; init; }
    public required DateTime BookingTime { get; init; }
    public required string Currency { get; init; }
    public required List<ReceiptLine> Lines { get; init; }
    public required ReceiptSum Sum { get; init; }
    public required List<ReceiptTaxInformation> TaxInformation { get; init; }
    public required List<ReceiptVoucher> Vouchers { get; init; }

    public string? PretixOrderCode { get; init; }
    public string? FiskalyQrCode { get; init; }
    public long? FiskalyTransactionNumber { get; init; }
}