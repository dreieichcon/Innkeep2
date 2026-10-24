using Innkeep2.Models.Internal.Receipt;

namespace Innkeep2.Client.Components.Dialogs;

public enum CardPaymentOutcome
{
    Closed,
    SwitchToCash,
    Completed,
    GaveUp
}

public sealed record CardPaymentDialogResult(CardPaymentOutcome Outcome, TransactionReceipt? Receipt = null);
