using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Innkeep2.Models.Fiskaly.Tss;

[UsedImplicitly]
public sealed record FiskalyTssUpdateRequest
{
    [JsonPropertyName("state")]
    public required TssState State { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }
}