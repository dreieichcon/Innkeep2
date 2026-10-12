using Innkeep2.CredentialsDb.Interfaces;

namespace Innkeep2.CredentialsDb.Models;

public interface ICredentialDocument : ILiteDbItem
{
    string Name { get; set; }
    bool Active { get; set; }
}
