using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Innkeep2.Models.Pretix.Order;

[UsedImplicitly]
public sealed record PretixOrderCreatePosition
{
    [JsonPropertyName("positionid")]
    public int? PositionId { get; init; }

    [JsonPropertyName("item")]
    public required int Item { get; init; }

    [JsonPropertyName("variation")]
    public int? Variation { get; init; }

    [JsonPropertyName("price")]
    public decimal? Price { get; init; }

    [JsonPropertyName("attendee_name")] 
    public string? AttendeeName { get; init; } = "internal_pos";
}