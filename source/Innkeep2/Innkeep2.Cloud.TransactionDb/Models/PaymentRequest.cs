using Demolite.Db.Models;
using Innkeep2.Models.Shared;

namespace Innkeep2.Cloud.TransactionDb.Models;

public class PaymentRequest : AbstractDbItem
{
    public required Guid RequestId { get; set; }

    public required string OrderRequestJson { get; set; }

    public required decimal TotalAmount { get; set; }

    public required string Currency { get; set; }

    public PaymentRequestStatus Status { get; set; } = PaymentRequestStatus.Pending;

    public string? ClientTransactionId { get; set; }

    public string? CheckoutId { get; set; }

    public string? FailureReason { get; set; }

    public required DateTime CreatedAt { get; set; }

    public required DateTime UpdatedAt { get; set; }
}
