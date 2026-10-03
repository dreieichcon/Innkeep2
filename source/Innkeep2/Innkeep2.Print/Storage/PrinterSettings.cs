using LiteDB;

namespace Innkeep2.Print.Storage;

public sealed record PrinterSettings
{
    [BsonId]
    public int Id { get; init; } = 1;

    public required string IpAddress { get; set; }
    public int Port { get; set; } = 9100;

    /// <summary>
    /// Printable width in dots, used to size images. Receipt printers differ (384, 512, 576, ...).
    /// </summary>
    public int ImageWidthDots { get; set; } = 512;
}