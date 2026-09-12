using System.Text.Json.Serialization;

namespace Innkeep2.Models.Fiskaly.Tss;

public enum TssState
{
    [JsonStringEnumMemberName("CREATED")]
    Created,

    [JsonStringEnumMemberName("UNINITIALIZED")]
    Uninitialized,

    [JsonStringEnumMemberName("INITIALIZED")]
    Initialized,

    [JsonStringEnumMemberName("DISABLED")]
    Disabled,

    [JsonStringEnumMemberName("DELETED")]
    Deleted,

    [JsonStringEnumMemberName("DEFECTIVE")]
    Defective,

    [JsonStringEnumMemberName("EVICTED")]
    Evicted
}