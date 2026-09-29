using ESCPOS;
using Innkeep2.Models.Internal.Receipt;
using Innkeep2.Models.Shared;
using Innkeep2.Print.Printer;

namespace Innkeep2.Print.Formatter;

public static class ReceiptDocumentBuilder
{
    private const string Separator = "----------------------------------------";

    public static byte[] Build(TransactionReceipt receipt) => receipt.TransactionType switch
    {
       TransactionType.Sale => BuildSale(receipt),
       TransactionType.Refund => BuildRefund(receipt),
       TransactionType.Transfer => BuildTransfer(receipt),
       _ => throw new ArgumentOutOfRangeException(nameof(receipt), receipt.TransactionType, "Unknown transaction type.")
    };

    private static byte[] BuildSale(TransactionReceipt receipt)
    {
       var builder = new PrintDocumentBuilder();

       AddHeaderSection(builder, receipt);
       AddProductsSection(builder, receipt);
       AddSumSection(builder, receipt);
       AddTaxSection(builder, receipt);
       AddQrSection(builder, receipt);

       builder.Cut();

       var bytes = builder.GetBytes();

       return receipt.Vouchers.Count > 0
          ? Concat(bytes, BuildVouchers(receipt))
          : bytes;
    }

    private static byte[] BuildRefund(TransactionReceipt receipt)
    {
       var builder = new PrintDocumentBuilder();

       AddHeaderSection(builder, receipt);
       AddSumSection(builder, receipt);
       AddQrSection(builder, receipt);

       builder.Cut();

       return builder.GetBytes();
    }

    private static byte[] BuildTransfer(TransactionReceipt receipt)
    {
       var builder = new PrintDocumentBuilder();

       AddHeaderSection(builder, receipt);
       AddSumSection(builder, receipt);
       AddQrSection(builder, receipt);

       builder
          .AddEmptyLines(4)
          .AddLine(Separator)
          .AddLine("Kassenwart", Justification.Center)
          .AddEmptyLines(4)
          .AddLine(Separator)
          .AddLine("Empfänger", Justification.Center);

       builder.Cut();

       return builder.GetBytes();
    }

    private static void AddHeaderSection(PrintDocumentBuilder builder, TransactionReceipt receipt)
    {
       builder.AddTitle(receipt.Title);

       foreach (var line in receipt.Header.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
          builder.AddLine(line, Justification.Center);

       builder.AddLine(Separator);

       builder.AddEmphasizedLine(ReceiptFormatter.FormatReceiptTypeLabel(receipt));

       foreach (var line in ReceiptFormatter.FormatSubheading(receipt))
          builder.AddLine(line, Justification.Center);

       builder.AddLine(Separator);
    }

    private static void AddProductsSection(PrintDocumentBuilder builder, TransactionReceipt receipt)
    {
       foreach (var line in receipt.Lines)
          foreach (var printLine in ReceiptFormatter.FormatLines(line))
             builder.AddLine(printLine);

       builder.AddLine(Separator);
    }

    private static void AddSumSection(PrintDocumentBuilder builder, TransactionReceipt receipt)
    {
       foreach (var line in ReceiptFormatter.FormatSum(receipt))
          builder.AddLine(line, Justification.Center);

       builder.AddLine(Separator);
    }

    private static void AddTaxSection(PrintDocumentBuilder builder, TransactionReceipt receipt)
    {
       foreach (var line in ReceiptFormatter.FormatTaxInformation(receipt.TaxInformation))
          builder.AddLine(line);

       builder.AddLine(Separator);
    }

    private static void AddQrSection(PrintDocumentBuilder builder, TransactionReceipt receipt)
    {
       if (receipt.HasFiskalyQrCode)
          builder.AddQrCode(receipt.FiskalyQrCode!);
    }

    private static byte[] BuildVouchers(TransactionReceipt receipt)
    {
       var builder = new PrintDocumentBuilder();

       foreach (var voucher in receipt.Vouchers)
       {
          builder.AddTitle(receipt.Title);
          builder.AddEmptyLine();
          builder.AddLine(voucher.ItemName, Justification.Center);
          builder.AddEmptyLine();

          if (!string.IsNullOrEmpty(voucher.Secret))
             builder.AddQrCode(voucher.Secret);

          builder.Cut();
       }

       return builder.GetBytes();
    }

    private static byte[] Concat(byte[] first, byte[] second)
    {
       var result = new byte[first.Length + second.Length];
       Buffer.BlockCopy(first, 0, result, 0, first.Length);
       Buffer.BlockCopy(second, 0, result, first.Length, second.Length);
       return result;
    }
}