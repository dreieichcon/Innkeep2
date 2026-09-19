using System.Text.Json.Serialization;

namespace Innkeep2.Models.Fiskaly.Transaction;

[JsonConverter(typeof(JsonStringEnumConverter<TxState>))]
public enum TxState
{
    [JsonStringEnumMemberName("ACTIVE")]
    Active,

    [JsonStringEnumMemberName("CANCELLED")]
    Cancelled,

    [JsonStringEnumMemberName("FINISHED")]
    Finished
}