namespace Innkeep2.Models.Internal;

public sealed record TransferRequest
{
    public required Guid RequestId { get; init; }
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
}