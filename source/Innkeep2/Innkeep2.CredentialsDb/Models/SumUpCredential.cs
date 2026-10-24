namespace Innkeep2.CredentialsDb.Models;

public sealed class SumUpCredential : ICredentialDocument
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; set; }
    public bool Active { get; set; }
    public required string ApiKey { get; set; }
    public required string MerchantCode { get; set; }
    public required string ReaderId { get; set; }
    public required string PublicBaseUrl { get; set; }
    public string WebhookToken { get; init; } = Guid.NewGuid().ToString("N");
    public string? AffiliateAppId { get; set; }
    public string? AffiliateKey { get; set; }
}
