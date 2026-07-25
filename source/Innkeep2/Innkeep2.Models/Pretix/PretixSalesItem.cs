using System.Text.Json.Serialization;
using Innkeep2.Models.Pretix.Core;

namespace Innkeep2.Models.Pretix;

public sealed record PretixSalesItem
{
    [JsonPropertyName("id")]
    public required int Id { get; init; }

    [JsonPropertyName("name")]
    public required MultiLanguageString Name { get; init; }

    [JsonPropertyName("internal_name")]
    public string? InternalName { get; init; }

    [JsonPropertyName("default_price")]
    public required decimal DefaultPrice { get; init; }

    [JsonPropertyName("category")]
    public int? Category { get; init; }

    [JsonPropertyName("active")]
    public required bool Active { get; init; }

    [JsonPropertyName("description")]
    public MultiLanguageString? Description { get; init; }

    [JsonPropertyName("free_price")]
    public required bool FreePrice { get; init; }

    [JsonPropertyName("free_price_suggestion")]
    public decimal? FreePriceSuggestion { get; init; }

    [JsonPropertyName("tax_rate")]
    public required decimal TaxRate { get; init; }

    [JsonPropertyName("tax_rule")]
    public int? TaxRule { get; init; }

    [JsonPropertyName("admission")]
    public required bool Admission { get; init; }

    [JsonPropertyName("personalized")]
    public required bool Personalized { get; init; }

    [JsonPropertyName("position")]
    public required int Position { get; init; }

    [JsonPropertyName("picture")]
    public string? Picture { get; init; }

    [JsonPropertyName("all_sales_channels")]
    public required bool AllSalesChannels { get; init; }

    [JsonPropertyName("limit_sales_channels")]
    public required IReadOnlyList<string> LimitSalesChannels { get; init; }

    [JsonPropertyName("available_from")]
    public DateTimeOffset? AvailableFrom { get; init; }

    [JsonPropertyName("available_from_mode")]
    public required string AvailableFromMode { get; init; }

    [JsonPropertyName("available_until")]
    public DateTimeOffset? AvailableUntil { get; init; }

    [JsonPropertyName("available_until_mode")]
    public required string AvailableUntilMode { get; init; }

    [JsonPropertyName("hidden_if_item_available")]
    public int? HiddenIfItemAvailable { get; init; }

    [JsonPropertyName("hidden_if_item_available_mode")]
    public required string HiddenIfItemAvailableMode { get; init; }

    [JsonPropertyName("require_voucher")]
    public required bool RequireVoucher { get; init; }

    [JsonPropertyName("hide_without_voucher")]
    public required bool HideWithoutVoucher { get; init; }

    [JsonPropertyName("allow_cancel")]
    public required bool AllowCancel { get; init; }

    [JsonPropertyName("min_per_order")]
    public int? MinPerOrder { get; init; }

    [JsonPropertyName("max_per_order")]
    public int? MaxPerOrder { get; init; }

    [JsonPropertyName("checkin_attention")]
    public required bool CheckinAttention { get; init; }

    [JsonPropertyName("checkin_text")]
    public string? CheckinText { get; init; }

    [JsonPropertyName("original_price")]
    public decimal? OriginalPrice { get; init; }

    [JsonPropertyName("require_approval")]
    public required bool RequireApproval { get; init; }

    [JsonPropertyName("require_bundling")]
    public required bool RequireBundling { get; init; }

    [JsonPropertyName("require_membership")]
    public required bool RequireMembership { get; init; }

    [JsonPropertyName("require_membership_hidden")]
    public required bool RequireMembershipHidden { get; init; }

    [JsonPropertyName("require_membership_types")]
    public required IReadOnlyList<int> RequireMembershipTypes { get; init; }

    [JsonPropertyName("grant_membership_type")]
    public int? GrantMembershipType { get; init; }

    [JsonPropertyName("grant_membership_duration_like_event")]
    public required bool GrantMembershipDurationLikeEvent { get; init; }

    [JsonPropertyName("grant_membership_duration_days")]
    public required int GrantMembershipDurationDays { get; init; }

    [JsonPropertyName("grant_membership_duration_months")]
    public required int GrantMembershipDurationMonths { get; init; }

    [JsonPropertyName("validity_mode")]
    public string? ValidityMode { get; init; }

    [JsonPropertyName("validity_fixed_from")]
    public DateTimeOffset? ValidityFixedFrom { get; init; }

    [JsonPropertyName("validity_fixed_until")]
    public DateTimeOffset? ValidityFixedUntil { get; init; }

    [JsonPropertyName("validity_dynamic_duration_minutes")]
    public int? ValidityDynamicDurationMinutes { get; init; }

    [JsonPropertyName("validity_dynamic_duration_hours")]
    public int? ValidityDynamicDurationHours { get; init; }

    [JsonPropertyName("validity_dynamic_duration_days")]
    public int? ValidityDynamicDurationDays { get; init; }

    [JsonPropertyName("validity_dynamic_duration_months")]
    public int? ValidityDynamicDurationMonths { get; init; }

    [JsonPropertyName("validity_dynamic_start_choice")]
    public required bool ValidityDynamicStartChoice { get; init; }

    [JsonPropertyName("validity_dynamic_start_choice_day_limit")]
    public int? ValidityDynamicStartChoiceDayLimit { get; init; }

    [JsonPropertyName("generate_tickets")]
    public bool? GenerateTickets { get; init; }

    [JsonPropertyName("allow_waitinglist")]
    public required bool AllowWaitinglist { get; init; }

    [JsonPropertyName("issue_giftcard")]
    public required bool IssueGiftcard { get; init; }

    [JsonPropertyName("media_policy")]
    public string? MediaPolicy { get; init; }

    [JsonPropertyName("media_type")]
    public string? MediaType { get; init; }

    [JsonPropertyName("show_quota_left")]
    public bool? ShowQuotaLeft { get; init; }

    [JsonPropertyName("has_variations")]
    public required bool HasVariations { get; init; }

    [JsonPropertyName("variations")]
    public required IReadOnlyList<PretixItemVariation> Variations { get; init; }

    [JsonPropertyName("meta_data")]
    public required IReadOnlyDictionary<string, object?> MetaData { get; init; }
}

public sealed record PretixItemVariation
{
    [JsonPropertyName("id")]
    public required int Id { get; init; }

    [JsonPropertyName("value")]
    public required MultiLanguageString Name { get; init; }

    [JsonPropertyName("default_price")]
    public decimal? DefaultPrice { get; init; }

    [JsonPropertyName("price")]
    public required decimal Price { get; init; }

    [JsonPropertyName("free_price_suggestion")]
    public decimal? FreePriceSuggestion { get; init; }

    [JsonPropertyName("original_price")]
    public decimal? OriginalPrice { get; init; }

    [JsonPropertyName("active")]
    public required bool Active { get; init; }

    [JsonPropertyName("description")]
    public MultiLanguageString? Description { get; init; }

    [JsonPropertyName("checkin_attention")]
    public required bool CheckinAttention { get; init; }

    [JsonPropertyName("checkin_text")]
    public string? CheckinText { get; init; }

    [JsonPropertyName("require_approval")]
    public required bool RequireApproval { get; init; }

    [JsonPropertyName("require_membership")]
    public required bool RequireMembership { get; init; }

    [JsonPropertyName("require_membership_hidden")]
    public required bool RequireMembershipHidden { get; init; }

    [JsonPropertyName("require_membership_types")]
    public required IReadOnlyList<int> RequireMembershipTypes { get; init; }

    [JsonPropertyName("all_sales_channels")]
    public required bool AllSalesChannels { get; init; }

    [JsonPropertyName("limit_sales_channels")]
    public required IReadOnlyList<string> LimitSalesChannels { get; init; }

    [JsonPropertyName("available_from")]
    public DateTimeOffset? AvailableFrom { get; init; }

    [JsonPropertyName("available_from_mode")]
    public required string AvailableFromMode { get; init; }

    [JsonPropertyName("available_until")]
    public DateTimeOffset? AvailableUntil { get; init; }

    [JsonPropertyName("available_until_mode")]
    public required string AvailableUntilMode { get; init; }

    [JsonPropertyName("hide_without_voucher")]
    public required bool HideWithoutVoucher { get; init; }

    [JsonPropertyName("meta_data")]
    public required IReadOnlyDictionary<string, object?> MetaData { get; init; }

    [JsonPropertyName("position")]
    public required int Position { get; init; }
}
