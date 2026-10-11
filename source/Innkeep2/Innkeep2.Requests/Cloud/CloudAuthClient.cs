using System.Text.Json;
using Demolite.Core.Models;
using Innkeep2.Requests.Core;

namespace Innkeep2.Requests.Cloud;

public sealed class CloudAuthClient(HttpClient httpClient)
    : CoreApiClient(httpClient, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
{
    public Task<Result<Unit>> CheckAsync(CancellationToken ct = default)
        => GetAsync<Unit>("auth", ct);
}
