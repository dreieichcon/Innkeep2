using System.Text.Json.Serialization;

namespace Innkeep2.Models.Fiskaly.Transaction;

[JsonConverter(typeof(JsonStringEnumConverter<ReceiptType>))]
public enum ReceiptType
{
    [JsonStringEnumMemberName("RECEIPT")]
    Receipt,

    [JsonStringEnumMemberName("TRANSFER")]
    Transfer
}