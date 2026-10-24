using System.Text.Json.Serialization;

namespace Innkeep2.Models.SumUp;

public sealed record SumUpRefundRequest
{
    /// <summary>
    /// Amount in major units of the currency. Without an amount SumUp refunds the whole transaction.
    /// </summary>
    [JsonPropertyName("amount")]
    public decimal? Amount { get; init; }
}
