using Innkeep2.Database.Model;
using Innkeep2.Models.Shared;

namespace Innkeep2.Cloud.TransactionDb.Models;

public class Transaction : AbstractDbItem
{
    public required Guid RequestId { get; set; }
    
    public required string Title { get; set; }
    public required string Header { get; set; }

    public required TransactionType TransactionType { get; set; }

    public required DateTime BookingTime { get; set; }

    public required PaymentType PaymentType { get; set; }

    public required decimal TotalAmount { get; set; }
    
    public required decimal AmountGiven { get; set; }
    
    public required decimal AmountBack { get; set; }
    
    public required string Currency { get; set; }

    public required string RequestJson { get; set; }

    public string? PretixOrderJson { get; set; }
    
    public TransactionStepStatus PretixStatus { get; set; } = TransactionStepStatus.Pending;

    public string? FiskalyTransactionJson { get; set; }
    
    public TransactionStepStatus FiskalyStatus { get; set; } = TransactionStepStatus.Pending;
    
    public Guid? RefundRequestId { get; set; }
}