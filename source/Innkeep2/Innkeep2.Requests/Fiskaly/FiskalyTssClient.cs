using System.Text.Json;
using Innkeep2.Models.Core;
using Innkeep2.Models.Fiskaly;
using Innkeep2.Models.Fiskaly.Core;
using Innkeep2.Models.Fiskaly.Tss;
using Innkeep2.Requests.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Innkeep2.Requests.Fiskaly;

public sealed class FiskalyTssClient(
    HttpClient httpClient,
    [FromKeyedServices("fiskaly")] JsonSerializerOptions serializerOptions)
    : CoreApiClient(httpClient, serializerOptions)
{
    public Task<Result<FiskalyTss>> CreateAsync(Guid tssId, CancellationToken ct = default)
        => PutAsync<FiskalyTss>($"tss/{tssId}", null, ct);
    
    public Task<Result<FiskalyListResponse<FiskalyTss>>> GetAllAsync(CancellationToken ct = default)
        => GetAsync<FiskalyListResponse<FiskalyTss>>("tss", ct);
    
    public Task<Result<Unit>> ChangeAdminPinAsync(
        Guid tssId,
        string adminPuk,
        string newAdminPin,
        CancellationToken ct = default
    )
        => PatchAsync<Unit>($"tss/{tssId}/admin", new FiskalyAdminPinRequest
        {
            AdminPuk = adminPuk,
            NewAdminPin = newAdminPin
        }, ct);
}