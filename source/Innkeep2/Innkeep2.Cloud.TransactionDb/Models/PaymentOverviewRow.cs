using Innkeep2.Models.Shared;

namespace Innkeep2.Cloud.TransactionDb.Models;

public sealed record PaymentOverviewRow(
    PaymentRequest Payment,
    bool HasOrder,
    TransactionStepStatus? PretixStatus,
    TransactionStepStatus? FiskalyStatus
);
