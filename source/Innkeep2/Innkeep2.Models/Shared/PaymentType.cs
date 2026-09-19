using System.Text.Json.Serialization;

namespace Innkeep2.Models.Shared;

[JsonConverter(typeof(JsonStringEnumConverter<PaymentType>))]
public enum PaymentType
{
    [JsonStringEnumMemberName("CASH")]
    Cash,

    [JsonStringEnumMemberName("NON_CASH")]
    NonCash
}