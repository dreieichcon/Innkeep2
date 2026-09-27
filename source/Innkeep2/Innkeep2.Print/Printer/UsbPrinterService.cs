using Innkeep2.Print.Storage;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;

namespace Innkeep2.Print.Printer;

public sealed class UsbPrinterService(PrinterSettingsRepository settingsRepository)
{
    public void Print(byte[] data)
    {
        var settings = settingsRepository.GetOrCreate();

        using var context = new UsbContext();

        var device = context.List()
                         .FirstOrDefault(d => d.ProductId == settings.ProductId && d.VendorId == settings.VendorId)
                     ?? throw new InvalidOperationException("Printer not found via USB.");

        device.Open();
        device.ClaimInterface(device.Configs[0].Interfaces[0].Number);

        var writer = device.OpenEndpointWriter(WriteEndpointID.Ep01);
        writer.Write(data, 5000, out _);

        device.Close();
    }
    
    public void OpenDrawer()
    {
        var builder = new PrintDocumentBuilder();
        builder.OpenDrawer();

        Print(builder.GetBytes());
    }
    
    public void PrintTestPage()
    {
        var builder = new PrintDocumentBuilder();

        builder
            .AddEmptyLines(2)
            .AddTitle("Testdruck")
            .AddLine(new string('-', 42))
            .AddLine("Drucker erfolgreich verbunden.")
            .AddEmptyLine()
            .Cut();

        Print(builder.GetBytes());
    }
}