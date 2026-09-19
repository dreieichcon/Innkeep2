using Innkeep2.Database.Model;
using Innkeep2.Models.Shared;

namespace Innkeep2.Cloud.TransactionDb.Models;

public class Order : AbstractDbItem
{
    public required DateTime BookingTime { get; set; }

    public required PaymentType PaymentType { get; set; }
    public required decimal TotalAmount { get; set; }
    public required decimal AmountGiven { get; set; }
    public required decimal AmountBack { get; set; }
    public required string Currency { get; set; }

    public required string RequestJson { get; set; }

    public string? PretixOrderJson { get; set; }
    public OrderStepStatus PretixStatus { get; set; } = OrderStepStatus.Pending;

    public string? FiskalyTransactionJson { get; set; }
    public OrderStepStatus FiskalyStatus { get; set; } = OrderStepStatus.Pending;
}