using Innkeep2.Models.Core;
using Innkeep2.Models.Fiskaly.Core;
using Innkeep2.Models.Fiskaly.Tss;
using Innkeep2.Requests.Core;
using Innkeep2.Requests.Fiskaly;
using Serilog;

namespace Innkeep2.Services.Cloud.Fiskaly;

public sealed class TssService(FiskalyTssClient client)
{
    private readonly List<TssCredentialEntry> _credentials = [];
    private readonly Lock _lock = new();

    public Task<Result<FiskalyListResponse<FiskalyTss>>> GetAllAsync(CancellationToken ct = default)
        => client.GetAllAsync(ct);

    public bool HasPuk(Guid tssId)
    {
        lock (_lock)
            return _credentials.Any(x => x.TssId == tssId && !string.IsNullOrEmpty(x.AdminPuk));
    }

    public bool HasPin(Guid tssId)
    {
        lock (_lock)
            return _credentials.Any(x => x.TssId == tssId && !string.IsNullOrEmpty(x.AdminPin));
    }

    public TssCredentialEntry? GetCredentials(Guid tssId)
    {
        lock (_lock)
            return _credentials.FirstOrDefault(x => x.TssId == tssId);
    }

    public async Task<Result<FiskalyTss>> CreateAsync(Guid tssId, CancellationToken ct = default)
    {
        var result = await client.CreateAsync(tssId, ct);

        if (!result.IsSuccess) 
            return result;
        
        Log.Debug("Tss Created with Admin PUK: {PUK}", result.Value!.AdminPuk);
        
        UpsertCredentials(new TssCredentialEntry { TssId = tssId, AdminPuk = result.Value!.AdminPuk! });

        return result;
    }

    public async Task<Result<Unit>> SetAdminPinAsync(
        Guid tssId,
        string adminPuk,
        string newAdminPin,
        CancellationToken ct = default
    )
    {
        var result = await client.ChangeAdminPinAsync(tssId, adminPuk, newAdminPin, ct);

        if (result.IsSuccess)
            UpsertCredentials(new TssCredentialEntry { TssId = tssId, AdminPuk = adminPuk, AdminPin = newAdminPin });

        return result;
    }
    
    public async Task<Result<FiskalyTss>> DeployTssAsync(Guid tssId, CancellationToken ct = default)
    {
        var result = await client.UpdateAsync(tssId, TssState.Uninitialized, ct: ct);

        LogResult(tssId, result);

        return result;
    }
    
    public Task<Result<FiskalyTss>> InitializeTssAsync(Guid tssId, string description, CancellationToken ct = default)
        => ExecuteAdminOperationAsync(
            tssId,
            opCt => client.UpdateAsync(tssId, TssState.Initialized, description, opCt),
            ct
        );

    public Task<Result<FiskalyTss>> DisableTssAsync(Guid tssId, CancellationToken ct = default)
        => ExecuteAdminOperationAsync(
            tssId,
            opCt => client.UpdateAsync(tssId, TssState.Disabled, ct: opCt),
            ct
        );
    
    private async Task<Result<FiskalyTss>> ExecuteAdminOperationAsync(
        Guid tssId,
        Func<CancellationToken, Task<Result<FiskalyTss>>> operation,
        CancellationToken ct
    )
    {
        var credentials = GetCredentials(tssId);

        if (credentials?.AdminPin is not { } pin)
            return Result<FiskalyTss>.Failure(new Error("Tss.NotAuthenticated", $"No admin PIN stored for TSS '{tssId}'."));

        var authResult = await client.AuthenticateAdminAsync(tssId, pin, ct);

        if (!authResult.IsSuccess)
            return Result<FiskalyTss>.Failure(authResult.Error!);

        var result = await operation(ct);

        await client.LogoutAdminAsync(tssId, ct);

        LogResult(tssId, result);

        return result;
    }

    private static void LogResult(Guid tssId, Result<FiskalyTss> result)
    {
        if (result.IsSuccess)
            Log.Information("TSS {TssId} updated to state {State}", tssId, result.Value!.State);
        else
            Log.Warning("TSS {TssId} update failed: {Error}", tssId, result.Error!.Message);
    }

    public void UpsertCredentials(TssCredentialEntry entry)
    {
        lock (_lock)
        {
            _credentials.RemoveAll(x => x.TssId == entry.TssId);
            _credentials.Add(entry);
        }
    }
}