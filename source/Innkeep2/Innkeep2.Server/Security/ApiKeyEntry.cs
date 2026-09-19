using LiteDB;

namespace Innkeep2.Server.Security;

public sealed record ApiKeyEntry
{
    [BsonId]
    public required int Id { get; init; }

    public required string Name { get; init; }
    public required string KeyHash { get; init; }
    public required DateTime CreatedAt { get; init; }
    public bool Revoked { get; set; }
}