using System.Text.Json;
using Innkeep2.Cloud.TransactionDb.Models;
using Innkeep2.Cloud.TransactionDb.Repositories;
using Innkeep2.Models.Core;
using Innkeep2.Models.Fiskaly.Transaction;
using Innkeep2.Models.Internal;
using Innkeep2.Models.Internal.Receipt;
using Innkeep2.Models.Pretix.Order;
using Innkeep2.Services.Cloud;
using Innkeep2.Services.Cloud.Fiskaly;
using Innkeep2.Services.Cloud.Pretix;
using Innkeep2.Services.Shared;
using Serilog;

namespace Innkeep2.Cloud.Services;

public sealed class OrderService(
    PretixOrderService pretixOrderService,
    FiskalyTransactionService fiskalyTransactionService,
    OrderRepository orderRepository,
    IActiveConfigurationService activeConfiguration
)
{
    public async Task<Result<TransactionReceipt>> CreateOrderAsync(OrderRequest request, CancellationToken ct = default)
    {
        if (activeConfiguration.Organizer is not { } organizer || activeConfiguration.Event is not { } pretixEvent)
            return Result<TransactionReceipt>.Failure(
                new Error("Order.NoConfiguration", "No organizer or event is currently selected."));

        var order = new Order
        {
            RequestId =  request.RequestId,
            BookingTime = DateTime.UtcNow,
            PaymentType = request.PaymentType,
            TotalAmount = request.AmountNeeded,
            AmountGiven = request.AmountGiven,
            AmountBack = request.AmountBack,
            Currency = request.Currency,
            RequestJson = JsonSerializer.Serialize(request)
        };

        var createResult = await orderRepository.CreateAsync(order, ct);

        if (!createResult.IsSuccess)
            return Result<TransactionReceipt>.Failure(createResult.Error!);

        order = createResult.Value!;

        var pretixOrder = await TryCreatePretixOrderAsync(order, organizer.Slug, pretixEvent, request, ct);
        var fiskalyTransaction = await TryCreateFiskalyTransactionAsync(order, request, ct);

        await orderRepository.UpdateAsync(order, ct);

        var receipt = ReceiptBuilder.Build(order.RequestId, order.BookingTime, request, pretixOrder, fiskalyTransaction);

        return Result<TransactionReceipt>.Success(receipt);
    }
    
    private async Task<PretixOrderResponse?> TryCreatePretixOrderAsync(
        Order order,
        string organizerSlug,
        Event eventInternal,
        OrderRequest request,
        CancellationToken ct
    )
    {
        var result = await pretixOrderService.CreateOrderAsync(organizerSlug, eventInternal.Slug, eventInternal.IsTestMode, request.Items, ct);

        if (!result.IsSuccess)
        {
            order.PretixStatus = OrderStepStatus.Failed;
            Log.Warning("Pretix order creation failed for order {OrderId}: {Error}", order.RequestId, result.Error!.Message);
            return null;
        }

        order.PretixStatus = OrderStepStatus.Completed;
        order.PretixOrderJson = JsonSerializer.Serialize(result.Value);

        return result.Value;
    }
    
    private async Task<FiskalyTransaction?> TryCreateFiskalyTransactionAsync(
        Order order,
        OrderRequest request,
        CancellationToken ct
    )
    {
        var startResult = await fiskalyTransactionService.StartAsync(order.RequestId, ct);

        if (!startResult.IsSuccess)
        {
            order.FiskalyStatus = OrderStepStatus.Failed;
            Log.Warning("Fiskaly transaction start failed for order {OrderId}: {Error}", order.RequestId, startResult.Error!.Message);
            return null;
        }

        var finishResult = await fiskalyTransactionService.FinishAsync(order.RequestId, revision: 2, request, ct);

        if (!finishResult.IsSuccess)
        {
            order.FiskalyStatus = OrderStepStatus.Failed;
            Log.Warning("Fiskaly transaction finish failed for order {OrderId}: {Error}", order.RequestId, finishResult.Error!.Message);
            return null;
        }

        order.FiskalyStatus = OrderStepStatus.Completed;
        order.FiskalyTransactionJson = JsonSerializer.Serialize(finishResult.Value);

        return finishResult.Value;
    }
}