using System.Text.Json.Serialization;
using Innkeep2.Models.Serialization.Fiskaly;
using JetBrains.Annotations;

namespace Innkeep2.Models.Fiskaly.Transaction;

[UsedImplicitly]
public sealed record FiskalyAmountPerVatRate
{
    [JsonPropertyName("vat_rate")]
    public required VatRate VatRate { get; init; }

    [JsonConverter(typeof(FiskalyDecimalConverter))]
    [JsonPropertyName("amount")]
    public required decimal Amount { get; init; }
}