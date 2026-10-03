using Innkeep2.Models.Internal.Receipt;

namespace Innkeep2.Services.Client;

/// <summary>
/// What the customer facing display currently shows besides the cart.
/// </summary>
public sealed class CustomerDisplayService
{
    public event EventHandler? Changed;

    /// <summary>
    /// The receipt of the payment that was just completed, or null while a cart is being built.
    /// </summary>
    public TransactionReceipt? CompletedReceipt { get; private set; }

    /// <summary>
    /// Link to a digital receipt, shown as a QR code next to the payment summary. Nothing sets this yet.
    /// </summary>
    public string? DigitalReceiptUrl { get; private set; }

    public void ShowPaymentComplete(TransactionReceipt receipt, string? digitalReceiptUrl = null)
    {
        CompletedReceipt = receipt;
        DigitalReceiptUrl = digitalReceiptUrl;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void ShowCart()
    {
        CompletedReceipt = null;
        DigitalReceiptUrl = null;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
