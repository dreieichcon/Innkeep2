using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Innkeep2.Models.Fiskaly.Transaction;

[UsedImplicitly]
public sealed record FiskalyStandardV1Schema
{
    [JsonPropertyName("receipt")]
    public FiskalyReceipt? Receipt { get; init; }
}

[UsedImplicitly]
public sealed record FiskalyTransactionSchema
{
    [JsonPropertyName("standard_v1")]
    public FiskalyStandardV1Schema? StandardV1 { get; init; }
}