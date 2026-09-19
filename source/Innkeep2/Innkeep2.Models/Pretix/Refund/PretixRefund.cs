using System.Text.Json.Serialization;
using Innkeep2.Models.Serialization.Pretix;
using JetBrains.Annotations;

namespace Innkeep2.Models.Pretix.Refund;

[UsedImplicitly]
public sealed record PretixRefund
{
    [JsonPropertyName("local_id")]
    public required int LocalId { get; init; }

    [JsonPropertyName("state")]
    public required string State { get; init; }

    [JsonPropertyName("amount")]
    [JsonConverter(typeof(PretixDecimalConverter))]
    public required decimal Amount { get; init; }

    [JsonPropertyName("payment")]
    public required int Payment { get; init; }

    [JsonPropertyName("provider")]
    public required string Provider { get; init; }
}