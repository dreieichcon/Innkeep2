using Innkeep2.CredentialsDb.Models;

namespace Innkeep2.CredentialsDb.Repositories;

public sealed class SumUpCredentialRepository(string databasePath)
    : CredentialRepository<SumUpCredential>(databasePath, "sumup");
