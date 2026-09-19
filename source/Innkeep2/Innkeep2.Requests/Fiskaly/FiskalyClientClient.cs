using System.Text.Json;
using Innkeep2.Models.Core;
using Innkeep2.Models.Fiskaly.Client;
using Innkeep2.Models.Fiskaly.Core;
using Innkeep2.Requests.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Innkeep2.Requests.Fiskaly;

public sealed class FiskalyClientClient(
    HttpClient httpClient,
    [FromKeyedServices("fiskaly")] JsonSerializerOptions serializerOptions)
    : CoreApiClient(httpClient, serializerOptions)
{
    public Task<Result<FiskalyListResponse<FiskalyClient>>> GetAllAsync(CancellationToken ct = default)
        => GetAsync<FiskalyListResponse<FiskalyClient>>("client", ct);

    public Task<Result<FiskalyListResponse<FiskalyClient>>> GetAllForTssAsync(Guid tssId,
        CancellationToken ct = default)
        => GetAsync<FiskalyListResponse<FiskalyClient>>($"tss/{tssId}/client", ct);

    public Task<Result<FiskalyClient>> CreateAsync(
        Guid tssId,
        Guid clientId,
        string serialNumber,
        CancellationToken ct = default
    )
        => PutAsync<FiskalyClient>($"tss/{tssId}/client/{clientId}", new FiskalyClientCreateRequest
        {
            SerialNumber = serialNumber
        }, ct);

    public Task<Result<FiskalyClient>> UpdateAsync(
        Guid tssId,
        Guid clientId,
        ClientState state,
        CancellationToken ct = default
    )
        => PatchAsync<FiskalyClient>($"tss/{tssId}/client/{clientId}", new FiskalyClientUpdateRequest
        {
            State = state
        }, ct);
}