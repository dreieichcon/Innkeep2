namespace Innkeep2.Server.Queue;

public sealed record QueuedRequest
{
    public required Guid RequestId { get; init; }
    public required QueuedRequestType Type { get; init; }
    public required string PayloadJson { get; init; }
    public required DateTime EnqueuedAt { get; init; }
}

public enum QueuedRequestType
{
    Order,
    Refund,
    Transfer
}