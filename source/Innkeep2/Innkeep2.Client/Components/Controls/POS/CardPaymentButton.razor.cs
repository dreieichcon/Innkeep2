using Innkeep2.Client.Components.Dialogs;
using Innkeep2.Client.Services;
using Innkeep2.Models.Internal.Receipt;
using Innkeep2.Services.Client;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Serilog;

namespace Innkeep2.Client.Components.Controls.POS;

public partial class CardPaymentButton : IDisposable
{
    [Inject]
    public ClientCardPaymentService CardPaymentService { get; set; } = null!;

    [Inject]
    public ClientCartService CartService { get; set; } = null!;

    [Inject]
    public PaymentCompletionService PaymentCompletion { get; set; } = null!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    public IDialogService DialogService { get; set; } = null!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = null!;

    protected override void OnInitialized()
        => CartService.Changed += OnCartChanged;

    public void Dispose()
        => CartService.Changed -= OnCartChanged;

    private async Task OpenDialogAsync()
    {
        var options = new DialogOptions { CloseOnEscapeKey = false, BackdropClick = false, CloseButton = false };
        var dialog = await DialogService.ShowAsync<CardPaymentDialog>(options: options);
        var result = await dialog.Result;

        if (result?.Data is not CardPaymentDialogResult outcome)
            return;

        switch (outcome.Outcome)
        {
            case CardPaymentOutcome.SwitchToCash:
                NavigationManager.NavigateTo("/Pay");
                break;
            case CardPaymentOutcome.Completed:
                await CompleteAsync(outcome.Receipt!);
                break;
            case CardPaymentOutcome.GaveUp:
                await ShowUnconfirmedNoticeAsync();
                break;
        }
    }

    private async Task CompleteAsync(TransactionReceipt receipt)
    {
        try
        {
            PaymentCompletion.PrintVouchersAndOpenDrawer(receipt);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Printing after the card payment failed");
            Snackbar.Add("Die Zahlung ist erfolgt, der Bon konnte aber nicht gedruckt werden.", Severity.Warning);
        }

        await PaymentCompletion.ShowCompletionAsync(receipt);
    }

    private async Task ShowUnconfirmedNoticeAsync()
        => await DialogService.ShowMessageBoxAsync(
            "Zahlung nicht bestätigt",
            "Die Zahlung konnte nicht bestätigt werden. Bitte in der SumUp Cloud prüfen, ob die Zahlung erfolgreich war. Der Warenkorb wurde nicht verändert.");

    private void OnCartChanged(object? sender, EventArgs e)
        => InvokeAsync(StateHasChanged);
}
