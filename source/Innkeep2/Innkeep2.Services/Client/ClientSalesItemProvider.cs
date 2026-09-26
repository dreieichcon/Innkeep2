using Innkeep2.Models.Core;
using Innkeep2.Models.Internal;
using Innkeep2.Requests.Server;

namespace Innkeep2.Services.Client;

public sealed class ClientSalesItemProvider(ServerDataClient client)
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(60);

    private IReadOnlyList<SalesItem>? _cached;

    public DateTimeOffset? LastUpdated { get; private set; }

    public event EventHandler? Changed;

    public Task<Result<IReadOnlyList<SalesItem>>> GetCachedItemsAsync(CancellationToken ct = default)
        => _cached is not null
            ? Task.FromResult(Result<IReadOnlyList<SalesItem>>.Success(_cached))
            : RefreshAsync(ct);

    public async Task StartPollingAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(CacheDuration);

        await RefreshAsync(ct);

        while (await timer.WaitForNextTickAsync(ct))
            await RefreshAsync(ct);
    }

    private async Task<Result<IReadOnlyList<SalesItem>>> RefreshAsync(CancellationToken ct)
    {
        var result = await client.GetSalesItemsAsync(ct);

        if (!result.IsSuccess)
            return result;

        _cached = result.Value;
        LastUpdated = DateTimeOffset.UtcNow;

        Changed?.Invoke(this, EventArgs.Empty);

        return result;
    }
}