namespace Innkeep2.CredentialsDb.Models;

public sealed class DiscordCredential : ICredentialDocument
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; set; }
    public bool Active { get; set; }
    public required string WebhookUrl { get; set; }
}
