using Innkeep2.CredentialsDb.Models;

namespace Innkeep2.Ui.Shared.Components.Config;

public partial class CredentialPairManager
{
    private const string DefaultPretixUrl = "https://pretix.eu/api/v1/";

    private static CredentialPair CreateNew() => new()
    {
        Name = string.Empty,
        Pretix = new PretixCredential(string.Empty, DefaultPretixUrl),
        Fiskaly = new FiskalyCredential(string.Empty, string.Empty)
    };

    private static CredentialPair Clone(CredentialPair source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        Active = source.Active,
        Pretix = source.Pretix,
        Fiskaly = source.Fiskaly
    };

    private static void SetPretixUrl(CredentialPair credential, string? value)
        => credential.Pretix = credential.Pretix with { BaseUrl = value ?? string.Empty };

    private static void SetPretixKey(CredentialPair credential, string? value)
        => credential.Pretix = credential.Pretix with { ApiKey = value ?? string.Empty };

    private static void SetFiskalyKey(CredentialPair credential, string? value)
        => credential.Fiskaly = credential.Fiskaly with { ApiKey = value ?? string.Empty };

    private static void SetFiskalySecret(CredentialPair credential, string? value)
        => credential.Fiskaly = credential.Fiskaly with { ApiSecret = value ?? string.Empty };
}
