using JetBrains.Annotations;

namespace Innkeep2.Credentials.Models;

[UsedImplicitly]
public sealed record CloudCredential(string Name, bool Active, bool IsTest = false) : ICredential
{
    public required string ApiKey { get; init; }
    public required string CloudUrl { get; init; }
}