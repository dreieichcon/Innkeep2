using System.Text.Json.Serialization;
using Innkeep2.Models.Pretix.Core;

namespace Innkeep2.Models.Pretix;

public record PtEvent
{
	[JsonPropertyName("name")]
    public required MultiLanguageString Name { get; init; }

    [JsonPropertyName("slug")]
    public string? Slug { get; init; }

    [JsonPropertyName("live")]
    public bool Live { get; init; }

    [JsonPropertyName("testmode")]
    public bool TestMode { get; init; }

    [JsonPropertyName("currency")]
    public string? Currency { get; init; }

    [JsonPropertyName("date_from")]
    public DateTime DateFrom { get; init; }

    [JsonPropertyName("date_to")]
    public DateTime? DateTo { get; init; }

    [JsonPropertyName("date_admission")]
    public DateTime? DateAdmission { get; init; }

    [JsonPropertyName("is_public")]
    public bool IsPublic { get; init; }

    [JsonPropertyName("presale_start")]
    public DateTime? PresaleStart { get; init; }

    [JsonPropertyName("presale_end")]
    public DateTime? PresaleEnd { get; init; }

    [JsonPropertyName("location")]
    public Dictionary<string, string>? Location { get; init; }

    [JsonPropertyName("geo_lat")]
    public float? GeoLat { get; init; }

    [JsonPropertyName("geo_lon")]
    public float? GeoLon { get; init; }

    [JsonPropertyName("has_subevents")]
    public bool HasSubevents { get; init; }

    [JsonPropertyName("meta_data")]
    public Dictionary<string, object>? MetaData { get; init; }

    [JsonPropertyName("plugins")]
    public List<string>? Plugins { get; init; }

    [JsonPropertyName("seating_plan")]
    public int? SeatingPlan { get; init; }

    [JsonPropertyName("seat_category_mapping")]
    public Dictionary<string, int?>? SeatCategoryMapping { get; init; }

    [JsonPropertyName("timezone")]
    public string? Timezone { get; init; }

    [JsonPropertyName("item_meta_properties")]
    public Dictionary<string, object>? ItemMetaProperties { get; init; }

    [JsonPropertyName("valid_keys")]
    public Dictionary<string, object>? ValidKeys { get; init; }

    [JsonPropertyName("all_sales_channels")]
    public bool AllSalesChannels { get; init; }

    [JsonPropertyName("limit_sales_channels")]
    public List<string>? LimitSalesChannels { get; init; }

    [JsonPropertyName("sales_channels")]
    public List<string>? SalesChannels { get; init; }

    [JsonPropertyName("public_url")]
    public string? PublicUrl { get; init; }
}