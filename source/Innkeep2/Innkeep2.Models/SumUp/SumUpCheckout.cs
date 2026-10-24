using System.Text.Json.Serialization;

namespace Innkeep2.Models.SumUp;

public sealed record SumUpMoney
{
    [JsonPropertyName("currency")]
    public required string Currency { get; init; }

    [JsonPropertyName("minor_unit")]
    public required int MinorUnit { get; init; }

    [JsonPropertyName("value")]
    public required long Value { get; init; }

    public static SumUpMoney FromDecimal(decimal amount, string currency)
        => new() { Currency = currency, MinorUnit = 2, Value = (long)Math.Round(amount * 100, MidpointRounding.AwayFromZero) };
}

public sealed record SumUpAffiliate
{
    [JsonPropertyName("app_id")]
    public required string AppId { get; init; }

    [JsonPropertyName("key")]
    public required string Key { get; init; }

    [JsonPropertyName("foreign_transaction_id")]
    public required string ForeignTransactionId { get; init; }
}

public sealed record SumUpCheckoutRequest
{
    [JsonPropertyName("total_amount")]
    public required SumUpMoney TotalAmount { get; init; }

    [JsonPropertyName("return_url")]
    public string? ReturnUrl { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("affiliate")]
    public SumUpAffiliate? Affiliate { get; init; }
}

public sealed record SumUpCheckoutResponse
{
    [JsonPropertyName("data")]
    public required SumUpCheckoutData Data { get; init; }
}

public sealed record SumUpCheckoutData
{
    [JsonPropertyName("client_transaction_id")]
    public required string ClientTransactionId { get; init; }

    [JsonPropertyName("checkout_id")]
    public string? CheckoutId { get; init; }
}
