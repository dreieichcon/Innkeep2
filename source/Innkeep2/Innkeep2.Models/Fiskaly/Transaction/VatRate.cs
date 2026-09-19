using System.Text.Json.Serialization;

namespace Innkeep2.Models.Fiskaly.Transaction;

[JsonConverter(typeof(JsonStringEnumConverter<VatRate>))]
public enum VatRate
{
    [JsonStringEnumMemberName("NORMAL")]
    Normal,

    [JsonStringEnumMemberName("REDUCED_1")]
    Reduced1,

    [JsonStringEnumMemberName("SPECIAL_RATE_1")]
    SpecialRate1,

    [JsonStringEnumMemberName("SPECIAL_RATE_2")]
    SpecialRate2,

    [JsonStringEnumMemberName("NULL")]
    Null
}