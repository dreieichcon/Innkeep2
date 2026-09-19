using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Innkeep2.Models.Pretix;

[UsedImplicitly]
public sealed record PretixQuota
{
    [JsonPropertyName("id")]
    public required int Id { get; init; }

    [JsonPropertyName("size")]
    public int? Size { get; init; }

    [JsonPropertyName("items")]
    public required IReadOnlyList<int> Items { get; init; }

    [JsonPropertyName("variations")]
    public required IReadOnlyList<int> Variations { get; init; }

    [JsonPropertyName("available")]
    public bool? Available { get; init; }

    [JsonPropertyName("available_number")]
    public int? AvailableNumber { get; init; }
}