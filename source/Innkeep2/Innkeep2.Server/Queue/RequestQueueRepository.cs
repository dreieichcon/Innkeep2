using LiteDB;

namespace Innkeep2.Server.Queue;

public sealed class RequestQueueRepository(string databasePath)
{
    public void Enqueue(QueuedRequest request)
    {
        using var db = new LiteDatabase(databasePath);
        db.GetCollection<QueuedRequest>("queue").Insert(request);
    }

    public IReadOnlyList<QueuedRequest> GetAll()
    {
        using var db = new LiteDatabase(databasePath);
        return db.GetCollection<QueuedRequest>("queue").FindAll().ToList();
    }

    public void Remove(Guid requestId)
    {
        using var db = new LiteDatabase(databasePath);
        db.GetCollection<QueuedRequest>("queue").DeleteMany(x => x.RequestId == requestId);
    }
}