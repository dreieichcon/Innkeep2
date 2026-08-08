using Innkeep2.Cloud.Database.Models;
using Innkeep2.Cloud.Database.Repositories;
using Innkeep2.Cloud.Services;
using Innkeep2.Database.Model;
using Innkeep2.Models.Internal;
using Innkeep2.Services.Cloud;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Innkeep2.Cloud.Components.Pages.Config;

public partial class PretixConfig
{
	# region Dependencies

	[Inject]
	private InnkeepCloudSettingsRepository SettingsRepository { get; set; } = null!;

	[Inject]
	private CachedOrganizerProvider OrganizerProvider { get; set; } = null!;

	[Inject]
	private CachedEventProvider EventProvider { get; set; } = null!;
	
	[Inject]
	private CachedSalesItemProvider SalesItemProvider { get; set; } = null!;

	[Inject]
	private ISnackbar Snackbar { get; set; } = null!;

	[Inject]
	private UiResultHandler Handler { get; set; } = null!;

	# endregion

	private InnkeepCloudSettings? Settings { get; set; }

	# region Organizer Selection

	private event EventHandler? OrganizerChanged;

	private Organizer[] Organizers { get; set; } = [];

	private Organizer? SelectedOrganizer
	{
		get;
		set
		{
			field = value;
			OrganizerChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	private async Task OnOrganizerChanged()
	{
		if (Settings?.PretixOrganizerSlug != SelectedOrganizer?.Slug)
		{
			Settings?.PretixOrganizerSlug = SelectedOrganizer?.Slug;
			Settings?.Operation = Operation.Update;
		}

		await LoadEvents();
	}

	# endregion

	# region Event Selection

	private event EventHandler? EventChanged;

	private Event[] Events { get; set; } = [];

	private Event? SelectedEvent
	{
		get;
		set
		{
			field = value;
			EventChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	private async Task OnEventChanged()
	{
		if (Settings?.PretixEventSlug != SelectedEvent?.Slug)
		{
			Settings?.PretixEventSlug = SelectedEvent?.Slug;
			Settings?.Operation = Operation.Update;
		}
		
		await LoadSalesItems();
	}

	#endregion

	private SalesItem[] SalesItems { get; set; } = [];
	
	public bool UseTestMode
	{
		get => Settings?.UseTestMode ?? false;
		set => Settings?.UseTestMode = value;
	}
	
	private bool HasChanges => Settings?.Operation == Operation.Update;
	
	protected override async Task OnInitializedAsync()
	{
		OrganizerChanged += async (_, _) => await OnOrganizerChanged();
		EventChanged += async (_, _) => await OnEventChanged();

		await LoadOrganizers();
		await LoadSettings();
	}

	private async Task LoadOrganizers()
	{
		var organizers = await Handler.TryExecuteAsync(
			() => OrganizerProvider.GetCachedItemsAsync(default),
			errorPrefix: "Failed to load organizers"
		);

		Organizers = organizers?.ToArray() ?? [];
	}

	private async Task LoadEvents()
	{
		if (SelectedOrganizer is null)
		{
			Events = [];
			return;
		}

		var events = await Handler.TryExecuteAsync(
			() => EventProvider.GetCachedItemsAsync(new EventKey(SelectedOrganizer.Slug)),
			errorPrefix: "Failed to load events"
		);

		Events = events?.ToArray() ?? [];
	}

	private async Task LoadSalesItems()
	{
		if (SelectedOrganizer is null || SelectedEvent is null)
		{
			SalesItems = [];
			return;
		}
		
		var salesItems = await Handler.TryExecuteAsync(
			() => SalesItemProvider.GetCachedItemsAsync(new SalesItemKey(SelectedOrganizer.Slug, SelectedEvent.Slug)),
			errorPrefix: "Failed to load sales items"
		);
		
		SalesItems =  salesItems?.ToArray() ?? [];
		await InvokeAsync(StateHasChanged);
	}

	private async Task LoadSettings()
	{
		Settings = await GetOrCreateSettings();

		if (Settings.PretixOrganizerSlug is not null)
		{
			SelectedOrganizer = Organizers.FirstOrDefault(o => o.Slug == Settings.PretixOrganizerSlug);
		}

		if (Settings.PretixEventSlug is not null)
		{
			SelectedEvent = Events.FirstOrDefault(o => o.Slug == Settings.PretixEventSlug);
		}
	}

	private async Task<InnkeepCloudSettings> GetOrCreateSettings()
	{
		var settings = (await SettingsRepository.GetAllAsync()).Value?.FirstOrDefault();

		if (settings is not null)
			return settings;

		var result = await SettingsRepository.CreateAsync(new InnkeepCloudSettings());

		if (result.IsSuccess)
			return (await SettingsRepository.GetAllAsync()).Value!.FirstOrDefault()!;

		Snackbar.Add($"Error in settings creation: {result.Error!.Message}", Severity.Error);
		return result.Value!;
	}

	private async Task SaveSettings()
	{
		if (Settings != null)
			await Handler.TryExecuteAsync(
				() => SettingsRepository.UpdateAsync(Settings),
				successMessage: "Settings saved successfully",
				errorPrefix: "Failed to save settings"
			);
	}
}