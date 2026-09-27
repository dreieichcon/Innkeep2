using Innkeep2.Models.Shared;

namespace Innkeep2.Models.Internal.Receipt;

public sealed record TransactionSummary
{
    public required Guid RequestId { get; init; }
    
    public required string? PretixOrderCode { get; init; }
    
    public required Guid? RefundRequestId { get; init; }
    
    public required DateTime BookingTime { get; init; }
    
    public required TransactionType TransactionType { get; init; }
    
    public required decimal TotalAmount { get; init; }
    
    public required TransactionStepStatus PretixStatus { get; init; }
    
    public required TransactionStepStatus FiskalyStatus { get; init; }
}