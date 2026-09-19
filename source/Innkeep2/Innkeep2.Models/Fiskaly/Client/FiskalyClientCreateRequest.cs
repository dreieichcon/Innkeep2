using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Innkeep2.Models.Fiskaly.Client;

[UsedImplicitly]
public sealed record FiskalyClientCreateRequest
{
    [JsonPropertyName("serial_number")]
    public required string SerialNumber { get; init; }
}