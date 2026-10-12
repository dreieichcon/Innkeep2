using Innkeep2.CredentialsDb.Models;

namespace Innkeep2.CredentialsDb.Repositories;

public sealed class DiscordCredentialRepository(string databasePath)
    : CredentialRepository<DiscordCredential>(databasePath, "discord");
