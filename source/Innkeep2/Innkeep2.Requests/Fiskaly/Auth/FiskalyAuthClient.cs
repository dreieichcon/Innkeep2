using System.Text.Json;
using Innkeep2.Models.Core;
using Innkeep2.Models.Fiskaly;
using Innkeep2.Requests.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Innkeep2.Requests.Fiskaly.Auth;

public sealed class FiskalyAuthClient(
    HttpClient httpClient,
    [FromKeyedServices("fiskaly")] JsonSerializerOptions serializerOptions
) : CoreApiClient(httpClient, serializerOptions)
{
    public Task<Result<FiskalyTokenResponse>> AuthenticateAsync(
        string apiKey,
        string apiSecret,
        CancellationToken ct = default
    )
        => PostAsync<FiskalyTokenResponse>("auth", new { api_key = apiKey, api_secret = apiSecret }, ct);
}