using Innkeep2.Server.Queue;

namespace Innkeep2.Server.Tests.Integration.Orders;

internal sealed class FakeRequestQueueRepository() : RequestQueueRepository(":memory:")
{
    private readonly List<QueuedRequest> _queued = [];

    public override void Enqueue(QueuedRequest request)
    {
        if (_queued.Any(x => x.RequestId == request.RequestId && x.Type == request.Type))
            return;

        _queued.Add(request);
    }

    public override IReadOnlyList<QueuedRequest> GetAll() => _queued.OrderBy(x => x.EnqueuedAt).ToList();

    public override QueuedRequest? Get(Guid id) => _queued.FirstOrDefault(x => x.Id == id);

    public override void Update(QueuedRequest request)
    {
        var index = _queued.FindIndex(x => x.Id == request.Id);

        if (index >= 0)
            _queued[index] = request;
    }

    public override void Remove(Guid id) => _queued.RemoveAll(x => x.Id == id);

    public bool WasEnqueued(Guid requestId) => _queued.Any(x => x.RequestId == requestId);
}