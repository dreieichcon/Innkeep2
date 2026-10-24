using Innkeep2.CredentialsDb.Models;

namespace Innkeep2.CredentialsDb.Repositories;

public sealed class SumUpReaderCredentialRepository(string databasePath)
    : CredentialRepository<SumUpReaderCredential>(databasePath, "sumupreaders");
