using Demolite.Core.Models;
using Innkeep2.Models.Internal;

namespace Innkeep2.Requests.Server;

public interface IServerPaymentClient
{
    Task<Result<PaymentStatusResponse>> StartCardPaymentAsync(OrderRequest request, CancellationToken ct = default);

    Task<Result<PaymentStatusResponse>> GetCardPaymentAsync(Guid requestId, CancellationToken ct = default);

    Task<Result<PaymentStatusResponse>> CancelCardPaymentAsync(Guid requestId, CancellationToken ct = default);
}
