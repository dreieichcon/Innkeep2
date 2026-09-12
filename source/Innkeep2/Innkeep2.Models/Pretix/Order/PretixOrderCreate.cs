using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Innkeep2.Models.Pretix.Order;

[UsedImplicitly]
public sealed record PretixOrderCreate
{
    [JsonPropertyName("status")]
    public string Status { get; init; } = "p";

    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("locale")]
    public required string Locale { get; init; }

    [JsonPropertyName("sales_channel")]
    public string SalesChannel { get; init; } = "pretixpos";

    [JsonPropertyName("payment_provider")]
    public string PaymentProvider { get; init; } = "manual";

    [JsonPropertyName("testmode")]
    public bool IsTestMode { get; init; }

    [JsonPropertyName("positions")]
    public required List<PretixOrderCreatePosition> Positions { get; init; }
}