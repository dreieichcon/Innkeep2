using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Innkeep2.Models.Fiskaly.Core;

[UsedImplicitly]
public sealed record FiskalyListResponse<T>
{
    [JsonPropertyName("data")]
    public required IReadOnlyList<T> Data { get; init; }

    [JsonPropertyName("count")]
    public required long Count { get; init; }
}