using Innkeep2.Models.Internal.Receipt;

namespace Innkeep2.Services.Client;

public interface ICompletedOrderRegistry
{
    void RegisterCompletedOrder(TransactionReceipt receipt);
}
