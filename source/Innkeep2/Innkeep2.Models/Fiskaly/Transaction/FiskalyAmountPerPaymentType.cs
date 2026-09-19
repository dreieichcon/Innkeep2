using System.Text.Json.Serialization;
using Innkeep2.Models.Serialization.Fiskaly;
using Innkeep2.Models.Shared;
using JetBrains.Annotations;

namespace Innkeep2.Models.Fiskaly.Transaction;

[UsedImplicitly]
public sealed record FiskalyAmountPerPaymentType
{
    [JsonPropertyName("payment_type")]
    public required PaymentType PaymentType { get; init; }

    [JsonPropertyName("amount")]
    [JsonConverter(typeof(FiskalyDecimalConverter))]
    public required decimal Amount { get; init; }

    [JsonPropertyName("currency_code")]
    public string? CurrencyCode { get; init; }
}