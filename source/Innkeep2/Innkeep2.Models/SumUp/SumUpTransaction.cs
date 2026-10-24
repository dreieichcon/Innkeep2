using System.Text.Json.Serialization;

namespace Innkeep2.Models.SumUp;

public sealed record SumUpTransaction
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("amount")]
    public decimal? Amount { get; init; }

    [JsonPropertyName("currency")]
    public string? Currency { get; init; }

    [JsonPropertyName("refunded_amount")]
    public decimal? RefundedAmount { get; init; }

    [JsonPropertyName("transaction_code")]
    public string? TransactionCode { get; init; }

    [JsonPropertyName("client_transaction_id")]
    public string? ClientTransactionId { get; init; }
}
