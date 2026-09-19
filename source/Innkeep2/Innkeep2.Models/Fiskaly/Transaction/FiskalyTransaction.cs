using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Innkeep2.Models.Fiskaly.Transaction;

[UsedImplicitly]
public sealed record FiskalyTransaction
{
    [JsonPropertyName("_id")]
    public required Guid Id { get; init; }

    [JsonPropertyName("number")]
    public required long Number { get; init; }

    [JsonPropertyName("state")]
    public required TxState State { get; init; }

    [JsonPropertyName("tss_id")]
    public required Guid TssId { get; init; }

    [JsonPropertyName("client_id")]
    public required Guid ClientId { get; init; }

    [JsonPropertyName("revision")]
    public required int Revision { get; init; }

    [JsonPropertyName("qr_code_data")]
    public string? QrCodeData { get; init; }

    [JsonPropertyName("time_start")]
    public required long TimeStart { get; init; }

    [JsonPropertyName("time_end")]
    public long? TimeEnd { get; init; }
}