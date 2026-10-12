namespace Innkeep2.CredentialsDb.Models;

public sealed class CredentialPair : ICredentialDocument
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; set; }
    public bool Active { get; set; }
    public required PretixCredential Pretix { get; set; }
    public required FiskalyCredential Fiskaly { get; set; }
}
