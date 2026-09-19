using System.Text.Json.Serialization;

namespace Innkeep2.Models.Fiskaly.Client;

[JsonConverter(typeof(JsonStringEnumConverter<ClientState>))]
public enum ClientState
{
    [JsonStringEnumMemberName("REGISTERED")]
    Registered,

    [JsonStringEnumMemberName("DEREGISTERED")]
    Deregistered
}