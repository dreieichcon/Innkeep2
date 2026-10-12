using Innkeep2.CredentialsDb.Models;

namespace Innkeep2.CredentialsDb.Repositories;

public sealed class InnkeepApiKeyRepository(string databasePath)
    : CredentialRepository<InnkeepApiKey>(databasePath, "apikeys");
