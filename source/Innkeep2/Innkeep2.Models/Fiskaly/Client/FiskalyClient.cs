using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Innkeep2.Models.Fiskaly.Client;

[UsedImplicitly]
public sealed record FiskalyClient
{
    [JsonPropertyName("_id")]
    public required Guid Id { get; init; }

    [JsonPropertyName("tss_id")]
    public required Guid TssId { get; init; }

    [JsonPropertyName("serial_number")]
    public required string SerialNumber { get; init; }

    [JsonPropertyName("state")]
    public required ClientState State { get; init; }

    [JsonPropertyName("time_creation")]
    public required long TimeCreation { get; init; }
}