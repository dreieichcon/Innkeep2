using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Innkeep2.Models.Pretix;

[UsedImplicitly]
public sealed record PretixEventSettings
{
    [JsonPropertyName("invoice_address_from_name")]
    public string InvoiceCompanyName { get; init; } = "";

    [JsonPropertyName("invoice_address_from")]
    public string InvoiceStreetAddress { get; init; } = "";

    [JsonPropertyName("invoice_address_from_zipcode")]
    public string InvoiceZipCode { get; init; } = "";

    [JsonPropertyName("invoice_address_from_city")]
    public string InvoiceCity { get; init; } = "";

    [JsonPropertyName("invoice_address_from_country")]
    public string InvoiceCountry { get; init; } = "";

    [JsonPropertyName("invoice_address_from_vat_id")]
    public string InvoiceVatId { get; init; } = "";
}