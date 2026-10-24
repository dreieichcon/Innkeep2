using System.Text.Json.Serialization;

namespace Innkeep2.Models.Shared;

[JsonConverter(typeof(JsonStringEnumConverter<PaymentRequestStatus>))]
public enum PaymentRequestStatus
{
    Pending,
    Paid,
    Completed,
    Failed,
    Cancelled,
    Expired
}
