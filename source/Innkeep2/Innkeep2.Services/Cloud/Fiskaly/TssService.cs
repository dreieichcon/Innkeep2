using Innkeep2.Models.Core;
using Innkeep2.Models.Fiskaly.Core;
using Innkeep2.Models.Fiskaly.Tss;
using Innkeep2.Requests.Core;
using Innkeep2.Requests.Fiskaly;

namespace Innkeep2.Services.Cloud.Fiskaly;

public sealed class TssService(FiskalyTssClient client)
{
    private readonly List<TssCredentialEntry> _credentials = [];
    private readonly Lock _lock = new();

    public Task<Result<FiskalyListResponse<FiskalyTss>>> GetAllAsync(CancellationToken ct = default)
        => client.GetAllAsync(ct);

    public bool IsAuthenticated(Guid tssId)
    {
        lock (_lock)
            return _credentials.Any(x => x.TssId == tssId && x.AdminPin is not null);
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
        
        lock (_lock)
            _credentials.Add(new TssCredentialEntry(tssId, result.Value!.AdminPuk!, null));

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

        if (!result.IsSuccess) 
            return result;
        
        lock (_lock)
            UpsertCredentials(tssId, adminPuk, newAdminPin);

        return result;
    }

    public void UpsertCredentials(Guid tssId, string adminPuk, string adminPin)
    {
        _credentials.RemoveAll(x => x.TssId == tssId);
        _credentials.Add(new TssCredentialEntry(tssId, adminPuk, adminPin));
    }
}