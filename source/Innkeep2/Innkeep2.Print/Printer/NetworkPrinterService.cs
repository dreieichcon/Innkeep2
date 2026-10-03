using System.Net.Sockets;
using Innkeep2.Print.Storage;

namespace Innkeep2.Print.Printer;

public sealed class NetworkPrinterService(PrinterSettingsRepository settingsRepository)
{
    private const int ChunkSize = 1024;
    private static readonly TimeSpan ChunkPause = TimeSpan.FromMilliseconds(10);

    public void Print(byte[] data)
    {
        var settings = settingsRepository.GetOrCreate();

        using var client = new TcpClient { NoDelay = true, SendTimeout = 10_000 };
        client.Connect(settings.IpAddress, settings.Port);

        using var stream = client.GetStream();

        for (var offset = 0; offset < data.Length; offset += ChunkSize)
        {
            if (offset > 0)
                Thread.Sleep(ChunkPause);

            stream.Write(data, offset, Math.Min(ChunkSize, data.Length - offset));
        }

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