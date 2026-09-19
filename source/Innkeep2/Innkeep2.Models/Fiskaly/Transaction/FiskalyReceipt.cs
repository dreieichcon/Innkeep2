using System.Text.Json.Serialization;

namespace Innkeep2.Models.Fiskaly.Transaction;

public sealed record FiskalyReceipt
{
    [JsonPropertyName("receipt_type")]
    public ReceiptType ReceiptType { get; init; } = ReceiptType.Receipt;

    [JsonPropertyName("amounts_per_vat_rate")]
    public required List<FiskalyAmountPerVatRate>? AmountsPerVatRate { get; init; }

    [JsonPropertyName("amounts_per_payment_type")]
    public required List<FiskalyAmountPerPaymentType> AmountsPerPaymentType { get; init; }
}