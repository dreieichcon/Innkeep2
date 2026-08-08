using Innkeep2.Cloud.Database.Models;
using Innkeep2.Cloud.Database.Repositories;
using Innkeep2.Services.Cloud;

namespace Innkeep2.Cloud.Services;

public sealed class StatusBarService(
	InnkeepCloudSettingsRepository settingsRepository,
	CachedOrganizerProvider organizerProvider,
	CachedEventProvider eventProvider
)
{
	public string? OrganizerName { get; private set; }

	public string? EventName { get; private set; }

	public event EventHandler? Changed;

	public async Task RefreshAsync(CancellationToken ct = default)
	{
		var settings = await settingsRepository.GetOrCreateAsync(() => new InnkeepCloudSettings(), ct);

		if (settings.Value is not { } settingsObject || string.IsNullOrEmpty(settingsObject.PretixOrganizerSlug))
		{
			OrganizerName = null;
			EventName = null;
			await NotifyChangedAsync();
			return;
		}

		var organizers = (await organizerProvider.GetCachedItemsAsync(default, ct)).Value;

		if (organizers is null)
		{
			await NotifyChangedAsync();
			return;
		}

		OrganizerName = organizers.FirstOrDefault(x => x.Slug == settingsObject.PretixOrganizerSlug)
			?.Name;

		var events = (await eventProvider.GetCachedItemsAsync(new EventKey(settingsObject.PretixOrganizerSlug!), ct))
			.Value;

		if (events is null)
		{
			await NotifyChangedAsync();
			return;
		}

		EventName = events.FirstOrDefault(e => e.Slug == settingsObject.PretixEventSlug)
			?.Name;

		await NotifyChangedAsync();
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