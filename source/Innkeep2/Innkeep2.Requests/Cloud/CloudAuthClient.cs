using System.Text.Json;
using Demolite.Core.Models;
using Demolite.Http.Client;
using Microsoft.Extensions.Logging;

namespace Innkeep2.Requests.Cloud;

public sealed class CloudAuthClient(HttpClient httpClient, ILogger<CloudAuthClient> logger)
    : ApiClient(httpClient, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, logger)
{
    public Task<Result> CheckAsync(CancellationToken ct = default)
        => GetAsync("auth", ct);
}
