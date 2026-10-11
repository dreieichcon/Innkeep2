using System.Text.Json;
using Demolite.Core.Models;
using Demolite.Http.Client;
using Innkeep2.Models.Fiskaly;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Innkeep2.Requests.Fiskaly.Auth;

public sealed class FiskalyAuthClient(
    HttpClient httpClient,
    [FromKeyedServices("fiskaly")] JsonSerializerOptions serializerOptions,
    ILogger<FiskalyAuthClient> logger
) : ApiClient(httpClient, serializerOptions, logger)
{
    public Task<Result<FiskalyTokenResponse>> AuthenticateAsync(
        string apiKey,
        string apiSecret,
        CancellationToken ct = default
    )
        => PostAsync<FiskalyTokenResponse>("auth", new { api_key = apiKey, api_secret = apiSecret }, ct);
}
