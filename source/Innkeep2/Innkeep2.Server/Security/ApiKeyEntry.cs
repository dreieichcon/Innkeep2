using Innkeep2.Credentials.Interfaces;
using LiteDB;

namespace Innkeep2.Server.Security;

public sealed record ApiKeyEntry : IApiKey
{
    [BsonId]
    public Guid Id { get; init; }

    public required string Name { get; set; }
    public required string KeyHash { get; init; }

    public required DateTime CreatedAt { get; set; }
    public bool Revoked { get; set; }
}