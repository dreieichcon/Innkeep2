using LiteDB;

namespace Innkeep2.Print.Storage;

public sealed record PrinterSettings
{
    [BsonId]
    public int Id { get; init; } = 1;

    public required int VendorId { get; set; }
    public required int ProductId { get; set; }
}