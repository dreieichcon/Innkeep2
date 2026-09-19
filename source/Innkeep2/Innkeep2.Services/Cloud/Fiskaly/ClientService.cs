using Innkeep2.Models.Core;
using Innkeep2.Models.Fiskaly.Client;
using Innkeep2.Models.Fiskaly.Core;
using Innkeep2.Requests.Fiskaly;
using Serilog;

namespace Innkeep2.Services.Cloud.Fiskaly;

public sealed class ClientService(FiskalyClientClient client, TssService tssService)
{
    
    
    public Task<Result<FiskalyListResponse<FiskalyClient>>> GetAllAsync(CancellationToken ct = default)
        => client.GetAllAsync(ct);
    
    public Task<Result<FiskalyListResponse<FiskalyClient>>> GetAllForTssAsync(Guid tssId, CancellationToken ct = default)
        => client.GetAllForTssAsync(tssId, ct);

    public async Task<Result<FiskalyClient>> CreateClientAsync(
        Guid tssId,
        Guid clientId,
        string serialNumber,
        CancellationToken ct = default
    )
    {
        var result = await tssService.ExecuteAdminOperationAsync(
            tssId,
            opCt => client.CreateAsync(tssId, clientId, serialNumber, opCt),
            ct
        );

        LogResult(tssId, result);
        return result;
    }

    public async Task<Result<FiskalyClient>> UpdateClientStateAsync(
        Guid tssId,
        Guid clientId,
        ClientState state,
        CancellationToken ct = default
    )
    {
        var result = await tssService.ExecuteAdminOperationAsync(
            tssId,
            opCt => client.UpdateAsync(tssId, clientId, state, opCt),
            ct
        );

        LogResult(tssId, result);
        return result;
    }

    private static void LogResult(Guid tssId, Result<FiskalyClient> result)
    {
        if (result.IsSuccess)
            Log.Information("Client for TSS {TssId} updated to state {State}", tssId, result.Value!.State);
    }
}