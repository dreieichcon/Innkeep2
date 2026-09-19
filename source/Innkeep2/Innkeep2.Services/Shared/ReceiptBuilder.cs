using Innkeep2.Models.Fiskaly.Transaction;
using Innkeep2.Models.Internal;
using Innkeep2.Models.Internal.Receipt;
using Innkeep2.Models.Pretix.Order;

namespace Innkeep2.Services.Shared;

public static class ReceiptBuilder
{
    private const string OfflinePretixPlaceholder = "PRETIX OFFLINE";
    private const string OfflineFiskalyPlaceholder = "TSS OFFLINE";

    public static TransactionReceipt Build(
        Guid orderId,
        DateTime bookingTime,
        Event pretixEvent,
        OrderRequest order,
        decimal amountGiven,
        decimal amountBack,
        PretixOrderResponse? pretixOrder = null,
        FiskalyTransaction? fiskalyTransaction = null
    ) => new()
    {
        OrderId = orderId,
        Title = pretixEvent.Name,
        Header = pretixEvent.Header ?? "",
        BookingTime = bookingTime,
        Currency = order.Currency,
        Lines = order.Items.Select(ReceiptLine.FromSalesItem).ToList(),
        Sum = new ReceiptSum
        {
            TotalAmount = order.AmountNeeded,
            AmountGiven = amountGiven,
            AmountReturned = amountBack
        },
        TaxInformation = BuildTaxInformation(order),
        Vouchers = BuildVouchers(order, pretixOrder),
        PretixOrderCode = pretixOrder?.Code ?? OfflinePretixPlaceholder,
        FiskalyQrCode = fiskalyTransaction?.QrCodeData ?? OfflineFiskalyPlaceholder,
        FiskalyTransactionNumber = fiskalyTransaction?.Number
    };

    public static TransactionReceipt BuildTransfer(
        Guid transactionId,
        DateTime bookingTime,
        Event pretixEvent,
        decimal amount,
        decimal amountGiven,
        decimal amountBack,
        string currency,
        FiskalyTransaction? fiskalyTransaction
    ) => new()
    {
        OrderId = transactionId,
        Title = pretixEvent.Name,
        Header = pretixEvent.Header ?? "",
        BookingTime = bookingTime,
        Currency = currency,
        Lines = [],
        Sum = new ReceiptSum { TotalAmount = amount, AmountGiven = amountGiven, AmountReturned = amountBack },
        TaxInformation = [],
        Vouchers = [],
        PretixOrderCode = null,
        FiskalyQrCode = fiskalyTransaction?.QrCodeData ?? "TSS OFFLINE",
        FiskalyTransactionNumber = fiskalyTransaction?.Number
    };

    private static List<ReceiptTaxInformation> BuildTaxInformation(OrderRequest order)
        => order.Items
            .GroupBy(x => x.TaxRate)
            .Select(g =>
            {
                var gross = g.Sum(x => x.Price);
                var tax = Math.Round(gross / (1 + g.Key / 100) * (g.Key / 100), 2);

                return new ReceiptTaxInformation
                {
                    TaxRate = g.Key,
                    Gross = gross,
                    TaxAmount = tax,
                    Net = gross - tax
                };
            })
            .ToList();

    private static List<ReceiptVoucher> BuildVouchers(OrderRequest order, PretixOrderResponse? pretixOrder)
    {
        if (pretixOrder is null)
            return [];

        var vouchers = new List<ReceiptVoucher>();

        foreach (var position in pretixOrder.Positions)
        {
            var matchingItem = order.Items.FirstOrDefault(item => Matches(item, position));

            if (matchingItem is { PrintCheckInVoucher: true })
                vouchers.Add(new ReceiptVoucher { ItemName = matchingItem.Name, Secret = position.Secret });
        }

        return vouchers;
    }

    private static bool Matches(SalesItem item, PretixOrderResponsePosition position)
    {
        var variation = item.VariationId == 0 ? null : item.VariationId as int?;
        return item.Id == position.Item && variation == position.Variation;
    }
}