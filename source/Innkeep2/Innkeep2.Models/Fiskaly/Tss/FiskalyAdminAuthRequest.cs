using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Innkeep2.Models.Fiskaly.Tss;

[UsedImplicitly]
public sealed record FiskalyAdminAuthRequest
{
    [JsonPropertyName("admin_pin")]
    public required string AdminPin { get; init; }
}