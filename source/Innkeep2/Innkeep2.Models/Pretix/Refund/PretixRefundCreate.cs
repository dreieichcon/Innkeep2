using System.Text.Json.Serialization;
using Innkeep2.Models.Serialization.Pretix;
using JetBrains.Annotations;

namespace Innkeep2.Models.Pretix.Refund;

[UsedImplicitly]
public sealed record PretixRefundCreate
{
    [JsonPropertyName("state")]
    public string State { get; init; } = "created";

    [JsonPropertyName("source")]
    public string Source { get; init; } = "admin";

    [JsonPropertyName("amount")]
    [JsonConverter(typeof(PretixDecimalConverter))]
    public required decimal Amount { get; init; }

    [JsonPropertyName("payment")]
    public required int Payment { get; init; }

    [JsonPropertyName("comment")]
    public string? Comment { get; init; }

    [JsonPropertyName("provider")]
    public string Provider { get; init; } = "manual";

    [JsonPropertyName("mark_canceled")]
    public bool MarkCanceled { get; init; }

    [JsonPropertyName("mark_pending")]
    public bool MarkPending { get; init; } = true;
}