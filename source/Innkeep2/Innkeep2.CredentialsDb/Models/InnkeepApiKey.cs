namespace Innkeep2.CredentialsDb.Models;

public sealed class InnkeepApiKey : ICredentialDocument
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; set; }
    public bool Active { get; set; }
    public required string ApiKey { get; set; }
    public required string Url { get; set; }
}
