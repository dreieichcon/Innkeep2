using System.Text.Json.Serialization;

namespace Innkeep2.Models.Shared;

[JsonConverter(typeof(JsonStringEnumConverter<PaymentTerminationReason>))]
public enum PaymentTerminationReason
{
    UserCancelled,
    Timeout
}
