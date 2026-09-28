using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Innkeep2.Models.Sun;

[UsedImplicitly]
public sealed record SunState
{
    [JsonPropertyName("date")]
    public DateOnly Date { get; init; }

    [JsonPropertyName("sunrise")]
    public TimeOnly Sunrise { get; init; }

    [JsonPropertyName("sunset")]
    public TimeOnly Sunset { get; init; }

    [JsonPropertyName("dusk")]
    public TimeOnly Dusk { get; init; }

    public bool IsAfterDark()
    {
        var now = TimeOnly.FromDateTime(DateTime.UtcNow);
        return now >= Sunset.AddHours(-1);
    }
}