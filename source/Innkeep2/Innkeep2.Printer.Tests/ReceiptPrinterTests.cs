using Innkeep2.Models.Internal.Receipt;
using Innkeep2.Models.Shared;
using Innkeep2.Print.Formatter;
using Innkeep2.Print.Printer;
using Innkeep2.Print.Storage;

namespace Innkeep2.Printer.Tests;

[TestClass]
public class ReceiptPrinterTests
{
    // Adjust to the actual VID/PID of the connected printer for local testing.
    private const int TestVendorId = 0x04B8;
    private const int TestProductId = 0x0202;

    private static string _dbPath = null!;
    private static PrinterSettingsRepository _repository = null!;
    private static UsbPrinterService _printerService = null!;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"printertest-{Guid.NewGuid()}.db");

        _repository = new PrinterSettingsRepository(_dbPath);
        _repository.Save(new PrinterSettings { VendorId = TestVendorId, ProductId = TestProductId });

        _printerService = new UsbPrinterService(_repository);
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }

    [TestMethod]
    public void Print_TestReceipt_PrintsSuccessfully()
    {
        var receipt = BuildTestReceipt();
        var bytes = ReceiptDocumentBuilder.Build(receipt);

        _printerService.Print(bytes);
    }
    
    private static TransactionReceipt BuildTestReceipt() => new()
    {
        OrderId = Guid.NewGuid(),
        TransactionType = TransactionType.Sale,
        Title = "Kassensoftware Test",
        Header = "Musterfirma GmbH\nMusterstraße 1\n12345 Musterstadt\n\nUst.Id: DE123456789\n",
        BookingTime = DateTime.UtcNow,
        Currency = "EUR",
        PaymentType = PaymentType.Cash,
        Lines =
        [
            new ReceiptLine { Name = "Bier 0,5l", UnitPrice = 4.50m, Quantity = 2, TaxClass = ReceiptTaxClass.A },
            new ReceiptLine { Name = "Bratwurst", UnitPrice = 3.80m, Quantity = 1, TaxClass = ReceiptTaxClass.A },
            new ReceiptLine { Name = "Eintritt", UnitPrice = 12.00m, Quantity = 1, TaxClass = ReceiptTaxClass.B }
        ],
        Sum = new ReceiptSum
        {
            TotalAmount = 24.80m,
            AmountGiven = 30.00m,
            AmountReturned = 5.20m
        },
        TaxInformation =
        [
            new ReceiptTaxInformation { TaxRate = 19m, Net = 10.08m, TaxAmount = 1.92m, Gross = 12.00m },
            new ReceiptTaxInformation { TaxRate = 7m, Net = 11.21m, TaxAmount = 0.79m, Gross = 12.00m }
        ],
        Vouchers = [],
        PretixOrderCode = "ABC12",
        FiskalyQrCode = "V0;955002-00;Kassenbeleg-V1;Beleg^0.00_2.55_0.00_0.00_0.00^2.55:Bar;18;112;test-signature-data",
        FiskalyTransactionNumber = 42
    };
}