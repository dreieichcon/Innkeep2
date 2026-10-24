using Innkeep2.Client.Components.Dialogs;
using Innkeep2.Models.Internal.Receipt;
using Innkeep2.Models.Shared;
using Innkeep2.Print.Formatter;
using Innkeep2.Print.Printer;
using Innkeep2.Services.Client;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Innkeep2.Client.Services;

public sealed class PaymentCompletionService(
    NetworkPrinterService printerService,
    IDialogService dialogService,
    CustomerDisplayService customerDisplayService,
    ClientCartService cartService,
    NavigationManager navigationManager)
{
    /// <summary>
    /// Prints the vouchers of the receipt and opens the cash drawer for cash payments.
    /// </summary>
    public void PrintVouchersAndOpenDrawer(TransactionReceipt receipt)
    {
        if (receipt.Vouchers.Any())
            printerService.Print(ReceiptDocumentBuilder.Build(receipt));

        if (receipt.PaymentType == PaymentType.Cash)
            printerService.OpenDrawer();
    }

    /// <summary>
    /// Shows the completed payment to the cashier and the customer, then clears the cart and returns to the cart page.
    /// </summary>
    public async Task ShowCompletionAsync(TransactionReceipt receipt)
    {
        var parameters = new DialogParameters<PaymentCompleteDialog>
        {
            { x => x.TransactionReceipt, receipt }
        };

        customerDisplayService.ShowPaymentComplete(receipt);

        DialogResult? dialogResult;

        try
        {
            var dialog = await dialogService.ShowAsync<PaymentCompleteDialog>(parameters);
            dialogResult = await dialog.Result;
        }
        finally
        {
            customerDisplayService.ShowCart();
        }

        if (dialogResult!.Canceled)
            return;

        cartService.Clear();
        navigationManager.NavigateTo("/");
    }
}
