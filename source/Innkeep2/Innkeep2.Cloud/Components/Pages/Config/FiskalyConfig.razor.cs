using Innkeep2.Models.Fiskaly.Client;
using Innkeep2.Models.Fiskaly.Tss;
using Innkeep2.Services.Cloud.Fiskaly;
using Microsoft.AspNetCore.Components;

namespace Innkeep2.Cloud.Components.Pages.Config;

public partial class FiskalyConfig
{
    [Inject] 
    public TssService TssService { get; set; } = null!;

    [Inject]
    public ClientService ClientService { get; set; } = null!;
    
    # region Tss
    private event EventHandler? TssChanged;
    
    private FiskalyTss[] TssEntries { get; set; } = [];

    private FiskalyTss? SelectedTss
    {
        get;
        set
        {
            if (ActiveConfiguration.Tss != value)
                _hasChanges = true;
            
            field = value;
            TssChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    
    # endregion
    
    # region Client
    
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
    
    # endregion

    private bool _hasChanges;

    protected override async Task OnInitializedAsync()
    {
        await LoadTss();
        await base.OnInitializedAsync();
        
        LoadSettings();

        TssChanged += async (_, _) => await LoadClients();
        SelectedTss = ActiveConfiguration.Tss;
        _hasChanges = false;
    }

    private void LoadSettings()
    {
        SelectedTss = ActiveConfiguration.Tss;
        SelectedClient = ActiveConfiguration.Client;
    }

    private async Task LoadTss()
    {
        var tssResult = await TssService.GetAllAsync();
        TssEntries = tssResult.Value?.Data.ToArray() ?? [];
    }

    private async Task LoadClients()
    {
        var clientResult = await ClientService.GetAllAsync();
        ClientEntries = clientResult.Value?.Data.ToArray() ?? [];
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
}