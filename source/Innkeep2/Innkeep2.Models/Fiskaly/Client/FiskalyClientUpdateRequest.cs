using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Innkeep2.Models.Fiskaly.Client;

[UsedImplicitly]
public sealed record FiskalyClientUpdateRequest
{
    [JsonPropertyName("state")]
    public required ClientState State { get; init; }
}