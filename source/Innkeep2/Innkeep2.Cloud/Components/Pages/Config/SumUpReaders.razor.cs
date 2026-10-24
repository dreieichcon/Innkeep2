using Innkeep2.Cloud.Services;
using Innkeep2.Cloud.Services.Payments;
using Innkeep2.CredentialsDb;
using Innkeep2.CredentialsDb.Models;
using Innkeep2.Models.SumUp;
using Innkeep2.Requests.SumUp;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Innkeep2.Cloud.Components.Pages.Config;

public partial class SumUpReaders : IDisposable
{
    private readonly Dictionary<Guid, SumUpConnectionCheckResult> _checks = [];
    private readonly Dictionary<string, string> _adoptNames = [];

    private IReadOnlyList<SumUpReaderCredential> _readers = [];
    private IReadOnlyList<SumUpReader>? _adoptable;
    private SumUpPairingResult? _pairingResult;
    private CancellationTokenSource? _pairingCancellation;
    private string _pairingCode = string.Empty;
    private string _readerName = string.Empty;
    private bool _isBusy;
    private bool _isPairing;

    [Inject]
    public SumUpReaderService ReaderService { get; set; } = null!;

    [Inject]
    public ActiveCredentialProvider<SumUpCredential> AccountCredentials { get; set; } = null!;

    [Inject]
    public UiResultHandler Handler { get; set; } = null!;

    [Inject]
    public IDialogService DialogService { get; set; } = null!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = null!;

    private bool HasAccount => AccountCredentials.HasActive;

    private string AccountText => AccountCredentials.Active is { } account
        ? $"Aktives Konto: {account.Name} ({account.MerchantCode})"
        : "Kein aktives Konto";

    private bool CanPair => HasAccount
                            && !_isPairing
                            && !string.IsNullOrWhiteSpace(_pairingCode)
                            && !string.IsNullOrWhiteSpace(_readerName);

    protected override async Task OnInitializedAsync()
        => await LoadReadersAsync();

    public void Dispose()
    {
        _pairingCancellation?.Cancel();
        _pairingCancellation?.Dispose();
    }

    private async Task LoadReadersAsync()
        => _readers = await Handler.TryExecuteAsync(
            () => ReaderService.GetSavedReadersAsync(),
            (IReadOnlyList<SumUpReaderCredential>)[],
            "Fehler beim Laden der Reader");

    private async Task ActivateAsync(SumUpReaderCredential reader)
    {
        _isBusy = true;

        await Handler.TryExecuteAsync(
            () => ReaderService.SetActiveAsync(reader),
            "Fehler beim Aktivieren",
            $"Reader '{reader.Name}' ist jetzt aktiv");

        await LoadReadersAsync();
        _isBusy = false;
    }

    private async Task CheckAsync(SumUpReaderCredential reader)
    {
        _isBusy = true;

        _checks[reader.Id] = await ReaderService.CheckReaderAsync(reader);

        _isBusy = false;
    }

    private async Task UnpairAsync(SumUpReaderCredential reader)
    {
        var confirmation = await DialogService.ShowMessageBoxAsync(
            "Reader entkoppeln",
            $"'{reader.Name}' wird bei SumUp gelöscht und hier entfernt. Danach muss der Reader am Gerät unter Connections, API über Disconnect getrennt werden.",
            yesText: "Entkoppeln",
            cancelText: "Abbrechen");

        if (confirmation is not true)
            return;

        _isBusy = true;

        var removed = await Handler.TryExecuteAsync(
            () => ReaderService.UnpairAsync(reader),
            "Fehler beim Entkoppeln",
            $"Reader '{reader.Name}' wurde entkoppelt. Bitte den Reader am Gerät noch trennen.");

        if (removed)
            _checks.Remove(reader.Id);

        await LoadReadersAsync();
        _isBusy = false;
    }

    private async Task PairAsync()
    {
        _pairingCancellation = new CancellationTokenSource();
        _pairingResult = null;
        _isPairing = true;

        try
        {
            _pairingResult = await ReaderService.PairAsync(_pairingCode, _readerName, _pairingCancellation.Token);
        }
        catch (OperationCanceledException)
        {
            Snackbar.Add("Kopplung abgebrochen", Severity.Info);
        }
        finally
        {
            _isPairing = false;
            _pairingCancellation.Dispose();
            _pairingCancellation = null;
        }

        if (_pairingResult is { IsPaired: true })
        {
            _pairingCode = string.Empty;
            _readerName = string.Empty;
            _adoptable = null;
            await LoadReadersAsync();
        }
    }

    private void CancelPairing()
        => _pairingCancellation?.Cancel();

    private async Task LoadAdoptableAsync()
    {
        _isBusy = true;

        _adoptable = await Handler.TryExecuteAsync(
            () => ReaderService.GetAdoptableReadersAsync(),
            errorPrefix: "Fehler beim Laden der Reader von SumUp");

        _isBusy = false;
    }

    private async Task AdoptAsync(SumUpReader reader)
    {
        _isBusy = true;

        var adopted = await Handler.TryExecuteAsync(
            () => ReaderService.AdoptReaderAsync(reader, AdoptName(reader)),
            "Reader übernommen",
            "Fehler beim Übernehmen");

        if (adopted is not null)
        {
            _adoptNames.Remove(reader.Id);
            _adoptable = _adoptable?.Where(x => x.Id != reader.Id).ToList();
            await LoadReadersAsync();
        }

        _isBusy = false;
    }

    private string AdoptName(SumUpReader reader)
        => _adoptNames.TryGetValue(reader.Id, out var name) ? name : reader.Name ?? reader.Id;

    private void SetAdoptName(SumUpReader reader, string? name)
        => _adoptNames[reader.Id] = name ?? string.Empty;

    private static Severity PairingSeverity(SumUpPairingResult result)
        => result.State switch
        {
            SumUpPairingState.Paired => Severity.Success,
            SumUpPairingState.InvalidInput
                or SumUpPairingState.CodeNotPending
                or SumUpPairingState.Expired
                or SumUpPairingState.TimedOut => Severity.Warning,
            _ => Severity.Error
        };

    private static Severity CheckSeverity(SumUpConnectionCheckResult result)
        => result switch
        {
            { State: SumUpConnectionState.Connected, ReaderOnline: true } => Severity.Success,
            { State: SumUpConnectionState.Connected } => Severity.Warning,
            { State: SumUpConnectionState.Incomplete } => Severity.Info,
            _ => Severity.Error
        };
}
