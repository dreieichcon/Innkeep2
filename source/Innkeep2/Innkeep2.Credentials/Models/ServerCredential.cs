using JetBrains.Annotations;

namespace Innkeep2.Credentials.Models;

[UsedImplicitly]
public sealed record ServerCredential
{
    public required string ApiKey { get; init; }
    public required string ServerUrl { get; init; }
}