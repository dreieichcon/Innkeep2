using LiteDB;

namespace Innkeep2.Server.Queue;

public sealed class QueuedRequest
{
    [BsonId]
    public Guid Id { get; init; } = Guid.NewGuid();

    public required Guid RequestId { get; init; }
    public required QueuedRequestType Type { get; init; }
    public required string PayloadJson { get; init; }
    public required DateTime EnqueuedAt { get; init; }

    public int Attempts { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public string? LastError { get; set; }
}

public enum QueuedRequestType
{
    Order,
    Refund,
    Transfer
}