using Innkeep2.Cloud.Services;
using Innkeep2.Models.Fiskaly.Client;
using Innkeep2.Models.Fiskaly.Tss;
using Innkeep2.Services.Cloud;
using Innkeep2.Services.Cloud.Fiskaly;
using Microsoft.AspNetCore.Components;

namespace Innkeep2.Cloud.Components.Pages.Config;

public partial class FiskalyConfig : ComponentBase
{
    [Inject] 
    public TssService TssService { get; set; } = null!;
    
    [Inject]
    private UiResultHandler Handler { get; set; } = null!;
    
    [Inject]
    private IActiveConfigurationService ActiveConfiguration { get; set; } = null!;
    
    private FiskalyTss[] TssEntries { get; set; } = [];

    private FiskalyTss? SelectedTss
    {
        get;
        set
        {
            if (ActiveConfiguration.Tss != value)
                _hasChanges = true;
            
            field = value;
        }
    }
    
    private FiskalyClient[] ClientEntries { get; set; } = [];

    private FiskalyClient? SelectedClient
    {
        get;
        set
        {
            if (ActiveConfiguration.Client != value)
                _hasChanges = true;
            field = value;
        }
    }

    private bool _hasChanges;

    protected override async Task OnInitializedAsync()
    {
        var result = await TssService.GetAllAsync();
        TssEntries = result.Value?.Data.ToArray() ?? [];
        SelectedTss = ActiveConfiguration.Tss;
        _hasChanges = false;
    }

    private async Task SaveSettings()
    {
        await Handler.TryExecuteAsync(
            () => ActiveConfiguration.SaveAsync(
                ActiveConfiguration.Organizer?.Slug,
                ActiveConfiguration.Event?.Slug,
                SelectedTss?.Id,
                SelectedClient?.Id,
                ActiveConfiguration.UseTestMode
            ),
            errorPrefix: "Failed to save settings"
        );
    }

    private async Task UpdateTss()
    {
       
    }
}