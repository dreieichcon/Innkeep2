using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Innkeep2.Models.Fiskaly.Transaction;

[UsedImplicitly]
public sealed record FiskalyTransactionUpdateRequest
{
    [JsonPropertyName("state")]
    public required TxState State { get; init; }

    [JsonPropertyName("client_id")]
    public required Guid ClientId { get; init; }

    [JsonPropertyName("schema")]
    public FiskalyTransactionSchema? Schema { get; init; }
}