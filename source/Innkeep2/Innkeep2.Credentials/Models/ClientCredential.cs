using JetBrains.Annotations;

namespace Innkeep2.Credentials.Models;

[UsedImplicitly]
public sealed record ClientCredential
{
    public required string ClientUrl { get; init; }
}