using Innkeep2.Cloud.Services;
using Innkeep2.Services.Cloud;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Innkeep2.Cloud.Components.Pages.Abstract;

public class AbstractUiPage : ComponentBase
{
	[Inject]
	protected UiResultHandler Handler { get; set; } = null!;
	
	[Inject]
	protected IActiveConfigurationService ActiveConfiguration { get; set; } = null!;
	
	[Inject]
	public IDialogService DialogService { get; set; } = null!;

	[Inject]
	public ISnackbar Snackbar { get; set; } = null!;

	protected override async Task OnInitializedAsync()
	{
		await ActiveConfiguration.RefreshAsync();
	}
}