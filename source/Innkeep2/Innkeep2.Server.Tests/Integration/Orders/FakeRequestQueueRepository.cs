using Innkeep2.Server.Queue;

namespace Innkeep2.Server.Tests.Integration.Orders;

internal sealed class FakeRequestQueueRepository() : RequestQueueRepository(":memory:")
{
    private readonly List<QueuedRequest> _queued = [];

    public override void Enqueue(QueuedRequest request) => _queued.Add(request);
    public override IReadOnlyList<QueuedRequest> GetAll() => _queued;
    public override void Remove(Guid requestId) => _queued.RemoveAll(x => x.RequestId == requestId);

    public bool WasEnqueued(Guid requestId) => _queued.Any(x => x.RequestId == requestId);
}