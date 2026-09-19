using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Innkeep2.Models.Fiskaly.Transaction;

[UsedImplicitly]
public sealed record FiskalyReceipt
{
    [JsonPropertyName("receipt_type")]
    public string ReceiptType { get; init; } = "RECEIPT";

    [JsonPropertyName("amounts_per_vat_rate")]
    public required List<FiskalyAmountPerVatRate> AmountsPerVatRate { get; init; }

    [JsonPropertyName("amounts_per_payment_type")]
    public required List<FiskalyAmountPerPaymentType> AmountsPerPaymentType { get; init; }
}