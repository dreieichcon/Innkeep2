using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Innkeep2.Models.Sun;

[UsedImplicitly]
public sealed record SunStateResponse
{
    [JsonPropertyName("results")]
    public required SunState SunState { get; set; }
}