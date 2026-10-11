using System.Text.Json;
using Demolite.Core.Models;
using Demolite.Http.Client;
using Microsoft.Extensions.Logging;

namespace Innkeep2.Requests.Server;

public sealed class ServerAuthClient(HttpClient httpClient, ILogger<ServerAuthClient> logger)
    : ApiClient(httpClient, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, logger)
{
    public Task<Result> CheckAsync(CancellationToken ct = default)
    {
        return GetAsync("auth", ct);
    }
}
