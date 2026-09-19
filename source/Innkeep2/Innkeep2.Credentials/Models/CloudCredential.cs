using JetBrains.Annotations;

namespace Innkeep2.Credentials.Models;


[UsedImplicitly]
public sealed record CloudCredential
{
    public required string ApiKey { get; init; }
    public required string CloudUrl { get; init; }
}