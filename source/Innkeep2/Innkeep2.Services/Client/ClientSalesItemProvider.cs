using Demolite.Core.Models;
using Innkeep2.Models.Internal;
using Innkeep2.Requests.Server;

namespace Innkeep2.Services.Client;

public sealed class ClientSalesItemProvider(ServerDataClient client)
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(5);

    private IReadOnlyList<SalesItem>? _cached;

    public DateTimeOffset? LastUpdated { get; private set; }

    public bool? IsStockFresh { get; private set; }

    public event EventHandler? Changed;

    public Task<Result<IReadOnlyList<SalesItem>>> GetCachedItemsAsync(CancellationToken ct = default)
        => _cached is not null
            ? Task.FromResult(Result<IReadOnlyList<SalesItem>>.Success(_cached))
            : ReloadAsync(ct);

    public async Task StartPollingAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(CacheDuration);

        await ReloadAsync(ct);

        while (await timer.WaitForNextTickAsync(ct))
            await ReloadAsync(ct);
    }

    /// <summary>
    /// Reads the items cached on the server without reloading them from Pretix.
    /// </summary>
    public async Task<Result<IReadOnlyList<SalesItem>>> ReloadAsync(CancellationToken ct = default)
    {
        var result = await client.GetSalesItemsAsync(ct);

        if (!result.IsSuccess)
            return result;

        StoreItems(result.Value!);

        return result;
    }

    /// <summary>
    /// Reloads items and stock from Pretix through Server and Cloud.
    /// </summary>
    public Task<Result<SalesItemsResult>> ForceRefreshAsync(CancellationToken ct = default)
        => RefreshFromServerAsync(client.RefreshSalesItemsAsync, ct);

    /// <summary>
    /// Reloads only the stock from Pretix through Server and Cloud.
    /// </summary>
    public Task<Result<SalesItemsResult>> RefreshStockAsync(CancellationToken ct = default)
        => RefreshFromServerAsync(client.RefreshSalesItemStockAsync, ct);

    private async Task<Result<SalesItemsResult>> RefreshFromServerAsync(
        Func<CancellationToken, Task<Result<SalesItemsResult>>> refresh,
        CancellationToken ct
    )
    {
        var result = await refresh(ct);

        if (!result.IsSuccess)
        {
            IsStockFresh = false;
            Changed?.Invoke(this, EventArgs.Empty);
            return result;
        }

        IsStockFresh = result.Value!.FromCloud;
        StoreItems(result.Value.Items);

        return result;
    }

    private void StoreItems(IReadOnlyList<SalesItem> items)
    {
        _cached = items;
        LastUpdated = DateTimeOffset.UtcNow;

        Changed?.Invoke(this, EventArgs.Empty);
    }
}
