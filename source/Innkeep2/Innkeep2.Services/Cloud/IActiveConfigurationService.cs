using Demolite.Core.Models;
using Innkeep2.Models.Fiskaly.Client;
using Innkeep2.Models.Fiskaly.Tss;
using Innkeep2.Models.Internal;

namespace Innkeep2.Services.Cloud;

public interface IActiveConfigurationService
{
    public Organizer? Organizer { get; }
    public Event? Event { get; }
    public FiskalyTss? Tss { get; }
    public FiskalyClient? Client { get; }
    public bool UseTestMode { get; set; }
    public string? OrderDatabasePath { get; }

    public event EventHandler? Changed;

    public Task<Result> SaveAsync(
        string? organizerSlug,
        string? eventSlug,
        Guid? tssId,
        Guid? clientId,
        bool useTestMode,
        string? databasePath,
        CancellationToken ct = default
    );

    public Task<Result> SaveAsync(CancellationToken ct = default);

    public Task<Result> RefreshAsync(CancellationToken ct = default);
    
    public Task SetTss(FiskalyTss tss);
    
    public Task SetClient(FiskalyClient client);
    
    public Task SetOrderDatabasePath(string? path);
    
    public Task<Result> ReloadSettingsForEventAsync(CancellationToken ct = default);

    public Task ChangedFromOutside();
}
