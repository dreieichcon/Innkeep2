using Demolite.Core.Models;
using Innkeep2.CredentialsDb;
using Innkeep2.CredentialsDb.Interfaces;
using Innkeep2.CredentialsDb.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Innkeep2.Ui.Shared.Components.Config;

public partial class CredentialManager<T> where T : class, ICredentialDocument
{
    private IReadOnlyList<T> _items = [];
    private T? _editing;
    private bool _isNew;
    private bool _showRestartHint;

    [Inject]
    public ICredentialRepository<T> Repository { get; set; } = null!;

    [Inject]
    public ActiveCredentialProvider<T> Provider { get; set; } = null!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = null!;

    [Inject]
    public IDialogService DialogService { get; set; } = null!;

    [Parameter]
    public string? Title { get; set; }

    [Parameter, EditorRequired]
    public Func<T> CreateNew { get; set; } = null!;

    [Parameter, EditorRequired]
    public Func<T, T> Clone { get; set; } = null!;

    [Parameter]
    public RenderFragment<T>? Fields { get; set; }

    [Parameter]
    public RenderFragment? ExtraColumns { get; set; }

    [Parameter]
    public bool AllowCreate { get; set; } = true;

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        var result = await Repository.GetAllAsync();
        _items = result.Value ?? [];
    }

    private void StartCreate()
    {
        _editing = CreateNew();
        _isNew = true;
    }

    private void StartEdit(T item)
    {
        _editing = Clone(item);
        _isNew = false;
    }

    private void Cancel() => _editing = null;

    private async Task SaveAsync()
    {
        if (_editing is not { } credential)
            return;

        var hadActive = Provider.HasActive;
        var result = _isNew ? await Repository.CreateAsync(credential) : await Repository.UpdateAsync(credential);

        if (!result.IsSuccess)
        {
            ReportError(result.Error!);
            return;
        }

        _editing = null;
        await ReloadAsync();
        _showRestartHint |= !hadActive && Provider.HasActive;
        Snackbar.Add("Zugangsdaten gespeichert", Severity.Success);
    }

    private async Task ActivateAsync(T item)
    {
        var result = await Repository.SetActiveAsync(item.Id);

        if (!result.IsSuccess)
        {
            ReportError(result.Error!);
            return;
        }

        await ReloadAsync();
    }

    private async Task DeleteAsync(T item)
    {
        var confirmed = await DialogService.ShowMessageBoxAsync(
            "Zugangsdaten löschen",
            $"'{item.Name}' wirklich löschen?",
            yesText: "Löschen",
            cancelText: "Abbrechen");

        if (confirmed != true)
            return;

        var result = await Repository.DeleteAsync(item.Id);

        if (!result.IsSuccess)
        {
            ReportError(result.Error!);
            return;
        }

        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        await Provider.ReloadAsync();
        await LoadAsync();
    }

    private void ReportError(Error error) => Snackbar.Add(error.Message, Severity.Error);
}
