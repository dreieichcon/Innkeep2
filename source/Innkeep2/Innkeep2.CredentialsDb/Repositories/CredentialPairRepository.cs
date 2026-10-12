using Innkeep2.CredentialsDb.Models;

namespace Innkeep2.CredentialsDb.Repositories;

public sealed class CredentialPairRepository(string databasePath)
    : CredentialRepository<CredentialPair>(databasePath, "credentialpairs");
