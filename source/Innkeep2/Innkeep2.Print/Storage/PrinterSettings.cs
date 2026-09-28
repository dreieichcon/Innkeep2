using LiteDB;

namespace Innkeep2.Print.Storage;

public sealed record PrinterSettings
{
    [BsonId]
    public int Id { get; init; } = 1;

    public required string IpAddress { get; set; }
    public int Port { get; set; } = 9100;
}