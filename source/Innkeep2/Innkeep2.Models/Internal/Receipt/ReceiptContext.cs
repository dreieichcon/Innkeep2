using Innkeep2.Models.Shared;

namespace Innkeep2.Models.Internal.Receipt;

public sealed record ReceiptContext(
    Guid OrderId,
    TransactionType TransactionType,
    DateTime BookingTime,
    string Title,
    string Header
);