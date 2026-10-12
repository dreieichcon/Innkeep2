namespace Innkeep2.CredentialsDb.Models;

public interface ICredentialDocument
{
    Guid Id { get; }
    string Name { get; set; }
    bool Active { get; set; }
}
