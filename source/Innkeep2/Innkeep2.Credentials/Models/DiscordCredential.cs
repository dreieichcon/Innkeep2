using JetBrains.Annotations;

namespace Innkeep2.Credentials.Models;

[UsedImplicitly]
public sealed record DiscordCredential
{
    public required string WebhookUrl { get; init; }
}