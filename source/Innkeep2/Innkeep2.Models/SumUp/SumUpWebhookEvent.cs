using System.Text.Json.Serialization;

namespace Innkeep2.Models.SumUp;

public sealed record SumUpWebhookEvent
{
    public const string SoloTransactionUpdated = "solo.transaction.updated";

    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("event_type")]
    public required string EventType { get; init; }

    [JsonPropertyName("payload")]
    public required SumUpWebhookPayload Payload { get; init; }

    [JsonPropertyName("timestamp")]
    public DateTimeOffset? Timestamp { get; init; }
}

public sealed record SumUpWebhookPayload
{
    [JsonPropertyName("client_transaction_id")]
    public required string ClientTransactionId { get; init; }

    [JsonPropertyName("merchant_code")]
    public string? MerchantCode { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("failure_reason")]
    public string? FailureReason { get; init; }
}
