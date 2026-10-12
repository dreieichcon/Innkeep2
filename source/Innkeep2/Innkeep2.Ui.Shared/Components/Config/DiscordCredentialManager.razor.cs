using Innkeep2.CredentialsDb.Models;

namespace Innkeep2.Ui.Shared.Components.Config;

public partial class DiscordCredentialManager
{
    private static DiscordCredential CreateNew() => new()
    {
        Name = string.Empty,
        WebhookUrl = string.Empty
    };

    private static DiscordCredential Clone(DiscordCredential source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        Active = source.Active,
        WebhookUrl = source.WebhookUrl
    };
}
