using System.Text.Json.Serialization;

namespace Innkeep2.Models.SumUp;

public sealed record SumUpTransaction
{
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("amount")]
    public decimal? Amount { get; init; }

    [JsonPropertyName("currency")]
    public string? Currency { get; init; }

    [JsonPropertyName("transaction_code")]
    public string? TransactionCode { get; init; }

    [JsonPropertyName("client_transaction_id")]
    public string? ClientTransactionId { get; init; }
}
