using Innkeep2.Models.Core;
using Innkeep2.Models.Fiskaly.Client;
using Innkeep2.Models.Fiskaly.Tss;
using Innkeep2.Models.Internal;
using Innkeep2.Requests.Core;
using Innkeep2.Services.Cloud;

namespace Innkeep2.Cloud.Tests.Integration.Orders;

internal sealed class FakeActiveConfigurationService : IActiveConfigurationService
{
    public Organizer? Organizer { get; set; }
    public Event? Event { get; set; }
    public FiskalyTss? Tss { get; set; }
    public FiskalyClient? Client { get; set; }
    public bool UseTestMode { get; set; }
    public string? OrderDatabasePath { get; set; }

    public event EventHandler? Changed;

    public async Task SetTss(FiskalyTss tss) => Tss = tss;
    public async Task SetClient(FiskalyClient client) => Client = client;
    public async Task SetOrderDatabasePath(string? path) => OrderDatabasePath = path;

    public Task<Result<Unit>> SaveAsync(CancellationToken ct = default)
        => Task.FromResult(Result<Unit>.Success(default));

    public Task<Result<Unit>> SaveAsync(
        string? organizerSlug, string? eventSlug, Guid? tssId, Guid? clientId, bool useTestMode,
        CancellationToken ct = default
    ) => Task.FromResult(Result<Unit>.Success(default));

    public Task<Result<Unit>> RefreshAsync(CancellationToken ct = default)
        => Task.FromResult(Result<Unit>.Success(default));
}