using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Innkeep2.Models.Pretix.Order;

[UsedImplicitly]
public sealed record PretixOrderResponse
{
    [JsonPropertyName("code")]
    public required string Code { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("secret")]
    public required string Secret { get; init; }

    [JsonPropertyName("event")]
    public required string Event { get; init; }

    [JsonPropertyName("positions")]
    public required List<PretixOrderResponsePosition> Positions { get; init; }
}