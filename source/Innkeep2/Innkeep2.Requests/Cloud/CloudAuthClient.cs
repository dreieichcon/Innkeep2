using System.Text.Json;
using Innkeep2.Models.Core;
using Innkeep2.Requests.Core;

namespace Innkeep2.Requests.Cloud;

public sealed class CloudAuthClient(HttpClient httpClient)
    : CoreApiClient(httpClient, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
{
    public Task<Result<Unit>> CheckAsync(CancellationToken ct = default)
        => GetAsync<Unit>("auth", ct);
}