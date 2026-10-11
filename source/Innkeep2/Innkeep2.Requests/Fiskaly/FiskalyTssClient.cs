using System.Text.Json;
using Demolite.Core.Models;
using Demolite.Http.Client;
using Innkeep2.Models.Fiskaly;
using Innkeep2.Models.Fiskaly.Core;
using Innkeep2.Models.Fiskaly.Tss;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Innkeep2.Requests.Fiskaly;

public sealed class FiskalyTssClient(
    HttpClient httpClient,
    [FromKeyedServices("fiskaly")] JsonSerializerOptions serializerOptions,
    ILogger<FiskalyTssClient> logger)
    : ApiClient(httpClient, serializerOptions, logger)
{
    public Task<Result<FiskalyTss>> CreateAsync(Guid tssId, CancellationToken ct = default)
        => PutAsync<FiskalyTss>($"tss/{tssId}", new {}, ct);
    
    public Task<Result<FiskalyListResponse<FiskalyTss>>> GetAllAsync(CancellationToken ct = default)
        => GetAsync<FiskalyListResponse<FiskalyTss>>("tss", ct);
    
    public Task<Result> AuthenticateAdminAsync(Guid tssId, string adminPin, CancellationToken ct = default)
        => PostAsync($"tss/{tssId}/admin/auth", new FiskalyAdminAuthRequest { AdminPin = adminPin }, ct);

    public Task<Result> LogoutAdminAsync(Guid tssId, CancellationToken ct = default)
        => PostAsync($"tss/{tssId}/admin/logout", new { }, ct);
    
    public Task<Result> ChangeAdminPinAsync(
        Guid tssId,
        string adminPuk,
        string newAdminPin,
        CancellationToken ct = default
    )
        => PatchAsync($"tss/{tssId}/admin", new FiskalyAdminPinRequest
        {
            AdminPuk = adminPuk,
            NewAdminPin = newAdminPin
        }, ct);
    
    public Task<Result<FiskalyTss>> UpdateAsync(
        Guid tssId,
        TssState state,
        string? description = null,
        CancellationToken ct = default
    )
        => PatchAsync<FiskalyTss>($"tss/{tssId}", new FiskalyTssUpdateRequest
        {
            State = state,
            Description = description
        }, ct);
}
