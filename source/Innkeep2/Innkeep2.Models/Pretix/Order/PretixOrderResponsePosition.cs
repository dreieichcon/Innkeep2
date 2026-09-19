using System.Text.Json.Serialization;
using Innkeep2.Models.Shared;
using JetBrains.Annotations;

namespace Innkeep2.Models.Pretix.Order;

[UsedImplicitly]
public sealed record PretixOrderResponsePosition
{
    [JsonPropertyName("id")]
    public required int Id { get; init; }

    [JsonPropertyName("positionid")]
    public required int PositionId { get; init; }

    [JsonPropertyName("item")]
    public required int Item { get; init; }

    [JsonPropertyName("variation")]
    public int? Variation { get; init; }

    [JsonPropertyName("price")]
    public required decimal Price { get; init; }

    [JsonPropertyName("attendee_name")]
    public string? AttendeeName { get; init; }

    [JsonPropertyName("secret")]
    public required string Secret { get; init; }
    
    [JsonIgnore]
    public bool PrintCheckInVoucher { get; set; }
}