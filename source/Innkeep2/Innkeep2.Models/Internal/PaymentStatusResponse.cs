using Innkeep2.Models.Internal.Receipt;
using Innkeep2.Models.Shared;

namespace Innkeep2.Models.Internal;

public sealed record PaymentStatusResponse(
    Guid RequestId,
    PaymentRequestStatus Status,
    string? Message = null,
    TransactionReceipt? Receipt = null
);
