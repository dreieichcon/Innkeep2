using Innkeep2.Models.Core;
using Innkeep2.Models.Fiskaly.Client;
using Innkeep2.Models.Fiskaly.Tss;
using Innkeep2.Models.Internal;
using Innkeep2.Requests.Core;

namespace Innkeep2.Services.Cloud;

public interface IActiveConfigurationService
{
    public Organizer? Organizer { get; }
    public Event? Event { get; }
    public FiskalyTss? Tss { get; }
    public FiskalyClient? Client { get; }
    
    public bool UseTestMode { get; }

    public event EventHandler? Changed;

    public Task<Result<Unit>> SaveAsync(
        string? organizerSlug,
        string? eventSlug,
        Guid? tssId,
        Guid? clientId,
        bool useTestMode,
        CancellationToken ct = default
    );

    public Task<Result<Unit>> RefreshAsync(CancellationToken ct = default);
    
    public Task SetTss(FiskalyTss tss);
    
    public Task SetClient(FiskalyClient client);
}