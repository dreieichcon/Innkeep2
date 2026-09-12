using Innkeep2.Cloud.Services;
using Innkeep2.Models.Internal;
using Innkeep2.Services.Cloud;
using Innkeep2.Services.Cloud.Cache;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Innkeep2.Cloud.Components.Pages.Config;

public partial class PretixConfig
{
	# region Dependencies

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

	[Inject]
	private IActiveConfigurationService ActiveConfiguration { get; set; } = null!;

	# endregion

	# region Organizer Selection

	private event EventHandler? OrganizerChanged;

	private Organizer[] Organizers { get; set; } = [];

	private Organizer? SelectedOrganizer
	{
		get;
		set
		{
			if (ActiveConfiguration.Organizer != value)
				_hasChanges = true;
			
			field = value;
			OrganizerChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	private async Task OnOrganizerChanged() => await LoadEvents();

	# endregion

	# region Event Selection

	private event EventHandler? EventChanged;

	private Event[] Events { get; set; } = [];

	private Event? SelectedEvent
	{
		get;
		set
		{
			if (ActiveConfiguration.Event != value)
				_hasChanges = true;
			
			field = value;
			EventChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	private async Task OnEventChanged() => await LoadSalesItems();

	#endregion

	private SalesItem[] SalesItems { get; set; } = [];

	public bool UseTestMode
	{
		get;
		set
		{
			if (ActiveConfiguration.UseTestMode != value)
				_hasChanges = true;
			
			field = value;
		}
	}

	private bool _hasChanges;

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

		SalesItems = salesItems?.ToArray() ?? [];
		await InvokeAsync(StateHasChanged);
	}

	private async Task LoadSettings()
	{
		await ActiveConfiguration.RefreshAsync();

		SelectedOrganizer = Organizers.FirstOrDefault(o => o.Slug == ActiveConfiguration.Organizer?.Slug);
		SelectedEvent = Events.FirstOrDefault(e => e.Slug == ActiveConfiguration.Event?.Slug);
		UseTestMode = ActiveConfiguration.UseTestMode;
	}

	private async Task SaveSettings()
	{
		await Handler.TryExecuteAsync(
			() => ActiveConfiguration.SaveAsync(
				SelectedOrganizer?.Slug,
				SelectedEvent?.Slug,
				ActiveConfiguration.Tss?.Id,
				UseTestMode
			),
			errorPrefix: "Failed to save settings"
		);
	}
}