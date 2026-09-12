using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Innkeep2.Models.Fiskaly.Tss;

[UsedImplicitly]
public sealed record FiskalyAdminPinRequest
{
    [JsonPropertyName("admin_puk")]
    public required string AdminPuk { get; init; }

    [JsonPropertyName("new_admin_pin")]
    public required string NewAdminPin { get; init; }
}