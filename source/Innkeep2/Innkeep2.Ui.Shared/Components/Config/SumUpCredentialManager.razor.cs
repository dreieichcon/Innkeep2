using Innkeep2.CredentialsDb.Models;

namespace Innkeep2.Ui.Shared.Components.Config;

public partial class SumUpCredentialManager
{
    private static SumUpCredential CreateNew() => new()
    {
        Name = string.Empty,
        ApiKey = string.Empty,
        MerchantCode = string.Empty,
        ReaderId = string.Empty,
        PublicBaseUrl = string.Empty
    };

    private static SumUpCredential Clone(SumUpCredential source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        Active = source.Active,
        ApiKey = source.ApiKey,
        MerchantCode = source.MerchantCode,
        ReaderId = source.ReaderId,
        PublicBaseUrl = source.PublicBaseUrl,
        WebhookToken = source.WebhookToken,
        AffiliateAppId = source.AffiliateAppId,
        AffiliateKey = source.AffiliateKey
    };

    private static string WebhookUrl(SumUpCredential credential)
        => $"{credential.PublicBaseUrl.TrimEnd('/')}/webhooks/sumup/{credential.WebhookToken}";
}
