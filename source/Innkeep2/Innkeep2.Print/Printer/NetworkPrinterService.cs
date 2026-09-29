using System.Net.Sockets;
using Innkeep2.Print.Storage;

namespace Innkeep2.Print.Printer;

public sealed class NetworkPrinterService(PrinterSettingsRepository settingsRepository)
{
    public void Print(byte[] data)
    {
        var settings = settingsRepository.GetOrCreate();

        using var client = new TcpClient();
        client.Connect(settings.IpAddress, settings.Port);

        using var stream = client.GetStream();
        stream.Write(data, 0, data.Length);
        stream.Flush();
    }

    public void PrintTestPage()
    {
        var builder = new PrintDocumentBuilder();

        builder
            .AddEmptyLines(2)
            .AddTitle("Testdruck")
            .AddLine(new string('-', 42))
            .AddLine("Drucker erfolgreich verbunden.")
            .AddEmptyLines(10)
            .Cut();

        Print(builder.GetBytes());
    }

    public void OpenDrawer()
    {
        var builder = new PrintDocumentBuilder();
        builder.OpenDrawer();

        Print(builder.GetBytes());
    }
}