using Innkeep2.Cloud.AppDb.Models;
using Innkeep2.Cloud.AppDb.Repositories;
using Innkeep2.Database.Model;
using Innkeep2.Models.Core;
using Innkeep2.Models.Fiskaly.Client;
using Innkeep2.Models.Fiskaly.Tss;
using Innkeep2.Models.Internal;
using Innkeep2.Requests.Core;
using Innkeep2.Requests.Pretix.Clients;
using Innkeep2.Services.Cloud;
using Innkeep2.Services.Cloud.Cache;
using Innkeep2.Services.Cloud.Fiskaly;

namespace Innkeep2.Cloud.Services;

public sealed class ActiveConfigurationService(
    InnkeepCloudSettingsRepository settingsRepository,
    PretixEventSettingsClient eventSettingsClient,
    CachedOrganizerProvider organizerProvider,
    CachedEventProvider eventProvider,
    TssService tssService,
    ClientService clientService
) : IActiveConfigurationService
{
    public Organizer? Organizer { get; private set; }
    public Event? Event { get; private set; }
    public FiskalyTss? Tss { get; private set; }
    public FiskalyClient? Client { get; private set; }
    public bool UseTestMode { get; set; }
    public string? OrderDatabasePath { get; private set; }

    public event EventHandler? Changed;

    public async Task<Result<Unit>> SaveAsync(
        string? organizerSlug,
        string? eventSlug,
        Guid? tssId,
        Guid? clientId,
        bool useTestMode,
        string? databasePath,
        CancellationToken ct = default
    )
    {
        var settingsResult = await settingsRepository.GetOrCreateAsync(() => new InnkeepCloudSettings(), ct);

        if (settingsResult.Value is not { } settings)
            return Result<Unit>.Failure(settingsResult.Error!);

        settings.PretixOrganizerSlug = organizerSlug;
        settings.PretixEventSlug = eventSlug;
        settings.SelectedTssId = tssId;
        settings.SelectedClientId = clientId;
        settings.UseTestMode = useTestMode;
        settings.SelectedOrderDatabasePath = databasePath;
        settings.Operation = Operation.Update;

        var updateResult = await settingsRepository.UpdateAsync(settings, ct);

        if (!updateResult.IsSuccess)
            return Result<Unit>.Failure(updateResult.Error!);

        return await RefreshAsync(ct);
    }

    public Task<Result<Unit>> SaveAsync(CancellationToken ct = default)
        => SaveAsync(Organizer?.Slug, Event?.Slug, Tss?.Id, Client?.Id, UseTestMode, OrderDatabasePath, ct);


    public async Task<Result<Unit>> RefreshAsync(CancellationToken ct = default)
    {
        var settingsResult = await settingsRepository.GetOrCreateAsync(() => new InnkeepCloudSettings(), ct);

        if (settingsResult.Value is not { } settings)
            return Result<Unit>.Failure(settingsResult.Error!);

        Organizer = await ResolveOrganizerAsync(settings.PretixOrganizerSlug, ct);
        Event = await ResolveEventAsync(settings.PretixOrganizerSlug, settings.PretixEventSlug, ct);
        await ReloadSettingsForEventAsync(ct);

        Tss = await ResolveTssAsync(settings.SelectedTssId, ct);
        Client = await ResolveClientAsync(settings.SelectedClientId, ct);
        UseTestMode = settings.UseTestMode;
        OrderDatabasePath = settings.SelectedOrderDatabasePath;

        await NotifyChangedAsync();

        return Result<Unit>.Success(default);
    }

    public async Task SetTss(FiskalyTss tss)
    {
        Tss = tss;
        await SaveAsync();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public async Task SetClient(FiskalyClient client)
    {
        Client = client;
        await SaveAsync();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public async Task SetOrderDatabasePath(string? path)
    {
        OrderDatabasePath = path;
        await SaveAsync();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public async Task<Result<Unit>> ReloadSettingsForEventAsync(CancellationToken ct = default)
    {
        if (Organizer is not { } organizer || Event is not { } currentEvent)
            return Result<Unit>.Failure(new Error("Configuration.NoEvent", "No event is currently selected."));

        var result = await eventSettingsClient.GetAsync(organizer.Slug, currentEvent.Slug, ct);

        if (!result.IsSuccess)
            return Result<Unit>.Failure(result.Error!);

        Event = currentEvent with { Header = Event.BuildHeader(result.Value!) };

        await NotifyChangedAsync();

        return Result<Unit>.Success(default);
    }

    public async Task ChangedFromOutside() => await NotifyChangedAsync();

    private async Task<Organizer?> ResolveOrganizerAsync(string? slug, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(slug))
            return null;

        var organizers = (await organizerProvider.GetCachedItemsAsync(default, ct)).Value;
        return organizers?.FirstOrDefault(x => x.Slug == slug);
    }

    private async Task<Event?> ResolveEventAsync(string? organizerSlug, string? eventSlug, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(organizerSlug) || string.IsNullOrEmpty(eventSlug))
            return null;

        var events = (await eventProvider.GetCachedItemsAsync(new EventKey(organizerSlug), ct)).Value;
        return events?.FirstOrDefault(x => x.Slug == eventSlug);
    }

    private async Task<FiskalyTss?> ResolveTssAsync(Guid? tssId, CancellationToken ct)
    {
        if (tssId is not { } id)
            return null;

        var tssList = (await tssService.GetAllAsync(ct)).Value;
        return tssList?.Data.FirstOrDefault(x => x.Id == id);
    }

    private async Task<FiskalyClient?> ResolveClientAsync(Guid? clientId, CancellationToken ct)
    {
        if (clientId is not { } id)
            return null;

        var clients = (await clientService.GetAllAsync(ct)).Value;
        return clients?.Data.FirstOrDefault(x => x.Id == id);
    }


    private Task NotifyChangedAsync()
    {
        try
        {
            Changed?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }
}