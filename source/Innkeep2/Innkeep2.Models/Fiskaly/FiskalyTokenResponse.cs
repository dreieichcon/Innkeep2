using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Innkeep2.Models.Fiskaly;

[UsedImplicitly]
public sealed record FiskalyTokenResponse
{
    [JsonPropertyName("access_token")]
    public required string AccessToken { get; init; }

    [JsonPropertyName("access_token_expires_in")]
    public required int AccessTokenExpiresIn { get; init; }

    [JsonPropertyName("refresh_token")]
    public required string RefreshToken { get; init; }

    [JsonPropertyName("refresh_token_expires_in")]
    public required int RefreshTokenExpiresIn { get; init; }
}