using Innkeep2.Services.Client;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Innkeep2.Client.Components.Dialogs;

public partial class CardPaymentDialog : IDisposable
{
    private readonly CancellationTokenSource _cancellation = new();

    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    [Inject]
    public ClientCardPaymentService CardPaymentService { get; set; } = null!;

    private CardPaymentSession Session { get; set; } = null!;

    private bool IsWorking => Session.Phase is CardPaymentPhase.Starting or CardPaymentPhase.Pending;

    private string StartText => Session.Phase == CardPaymentPhase.Failed ? "Erneut versuchen" : "Zahlung starten";

    private Severity StatusSeverity => Session.Phase switch
    {
        CardPaymentPhase.Completed => Severity.Success,
        CardPaymentPhase.Failed => Severity.Error,
        CardPaymentPhase.GaveUp => Severity.Warning,
        CardPaymentPhase.NotStarted => Severity.Info,
        _ => Severity.Normal
    };

    protected override void OnInitialized()
    {
        Session = CardPaymentService.CreateSession();
        Session.Changed += OnSessionChanged;
    }

    public void Dispose()
    {
        Session.Changed -= OnSessionChanged;
        _cancellation.Cancel();
        _cancellation.Dispose();
    }

    private async Task StartPaymentAsync()
    {
        try
        {
            await Session.StartAsync(_cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        FinishWhenDone();
    }

    private async Task CancelPaymentAsync()
    {
        try
        {
            await Session.CancelAsync(_cancellation.Token);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void FinishWhenDone()
    {
        switch (Session.Phase)
        {
            case CardPaymentPhase.Completed:
                MudDialog.Close(DialogResult.Ok(new CardPaymentDialogResult(CardPaymentOutcome.Completed, Session.Receipt)));
                break;
            case CardPaymentPhase.GaveUp:
                MudDialog.Close(DialogResult.Ok(new CardPaymentDialogResult(CardPaymentOutcome.GaveUp)));
                break;
        }
    }

    private void SwitchToCash()
        => MudDialog.Close(DialogResult.Ok(new CardPaymentDialogResult(CardPaymentOutcome.SwitchToCash)));

    private void Close()
        => MudDialog.Close(DialogResult.Ok(new CardPaymentDialogResult(CardPaymentOutcome.Closed)));

    private void OnSessionChanged(object? sender, EventArgs e)
        => InvokeAsync(StateHasChanged);
}
