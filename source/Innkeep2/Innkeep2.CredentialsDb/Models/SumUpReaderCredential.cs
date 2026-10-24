namespace Innkeep2.CredentialsDb.Models;

public sealed class SumUpReaderCredential : ICredentialDocument
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; set; }
    public bool Active { get; set; }
    public required string ReaderId { get; init; }
    public required string MerchantCode { get; init; }
    public string? DeviceModel { get; init; }
    public string? DeviceIdentifier { get; init; }
    public DateTime PairedAt { get; init; } = DateTime.UtcNow;
}
