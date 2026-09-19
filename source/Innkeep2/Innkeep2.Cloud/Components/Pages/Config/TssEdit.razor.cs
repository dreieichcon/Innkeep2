using Innkeep2.Cloud.Services;
using Innkeep2.Models.Fiskaly.Tss;
using Innkeep2.Services.Cloud;
using Innkeep2.Services.Cloud.Fiskaly;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Innkeep2.Cloud.Components.Pages.Config;

public partial class TssEdit
{
    [Inject]
    public TssService TssService { get; set; } = null!;

    [Inject]
    private UiResultHandler Handler { get; set; } = null!;

    [Inject]
    private IActiveConfigurationService ActiveConfiguration { get; set; } = null!;

    [Inject]
    private IDialogService DialogService { get; set; } = null!;

    private bool _isLoading;
    
    private TssCredentialEntry? ActiveCredentialEntry { get; set; } = null!;
    private FiskalyTss? ActiveTss => ActiveConfiguration.Tss;

    protected override void OnInitialized()
    {
        if (ActiveTss != null)
        {
            ActiveCredentialEntry = TssService.GetCredentials(ActiveTss.Id)
                                    ?? new TssCredentialEntry { TssId = ActiveTss.Id, AdminPuk = "", AdminPin = "" };
        }
    }

    private bool CanStorePin() => 
        ActiveCredentialEntry is not null && !string.IsNullOrEmpty(ActiveCredentialEntry.AdminPin);

    private bool CanStorePuk() => 
        ActiveCredentialEntry is not null && !string.IsNullOrEmpty(ActiveCredentialEntry.AdminPuk);

    private async Task StoreCredentials()
    {
        TssService.UpsertCredentials(ActiveCredentialEntry!);
        await InvokeAsync(StateHasChanged);
    }

    private async Task SetPin()
    {
        _isLoading = true;

        var result = await Handler.TryExecuteAsync(
            () => TssService.SetAdminPinAsync(ActiveTss!.Id, ActiveCredentialEntry!.AdminPuk,
                ActiveCredentialEntry!.AdminPin!),
            successMessage: "Pin gesetzt",
            errorPrefix: "Fehler beim setzen des Pin");

        _isLoading = false;
    }

    private async Task Deploy()
    {
        var confirmation =
            await DialogService.ShowMessageBoxAsync("Sicher?", "Achtung, dieser Schritt kann eine Weile dauern!");

        if (confirmation is not true)
            return;

        _isLoading = true;
        await InvokeAsync(StateHasChanged);

        var result = await Handler.TryExecuteAsync(
            () => TssService.DeployTssAsync(ActiveTss!.Id),
            successMessage: "Tss erfolgreich deployed",
            errorPrefix: "Fehler beim Deployen");

        _isLoading = false;
        await InvokeAsync(StateHasChanged);

        if (result is not null)
            await ActiveConfiguration.SetTss(result);
    }

    private bool CanInitialize()
    {
        return ActiveTss is not null && !string.IsNullOrEmpty(ActiveTss.Description);
    }

    private async Task Initialize()
    {
        var confirmation =
            await DialogService.ShowMessageBoxAsync("Sicher?", "Achtung, dieser Schritt verursacht Kosten!");
        
        if (confirmation is not true)
            return;

        _isLoading = true;
        await InvokeAsync(StateHasChanged);

        var result = await Handler.TryExecuteAsync(
            () => TssService.InitializeTssAsync(ActiveTss!.Id, ActiveTss!.Description!),
            successMessage: "Tss erfolgreich initialisiert",
            errorPrefix: "Fehler beim Initialisieren");

        _isLoading = false;
        await InvokeAsync(StateHasChanged);

        if (result is not null)
            await ActiveConfiguration.SetTss(result);
    }

    private async Task Deactivate()
    {
        var confirmation =
            await DialogService.ShowMessageBoxAsync("Sicher?", "Achtung, dieser Schritt ist permanent!");
        
        if (confirmation is not true)
            return;
        
        _isLoading = true;
        await InvokeAsync(StateHasChanged);

        var result = await Handler.TryExecuteAsync(
            () => TssService.DisableTssAsync(ActiveTss!.Id),
            successMessage: "Tss erfolgreich deaktiviert",
            errorPrefix: "Fehler beim deaktiviert");

        _isLoading = false;
        await InvokeAsync(StateHasChanged);

        if (result is not null)
            await ActiveConfiguration.SetTss(result);
    }
}