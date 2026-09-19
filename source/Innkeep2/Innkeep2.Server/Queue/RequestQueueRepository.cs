using LiteDB;

namespace Innkeep2.Server.Queue;

public class RequestQueueRepository(string databasePath)
{
    public virtual void Enqueue(QueuedRequest request)
    {
        using var db = new LiteDatabase(databasePath);
        db.GetCollection<QueuedRequest>("queue").Insert(request);
    }

    public virtual IReadOnlyList<QueuedRequest> GetAll()
    {
        using var db = new LiteDatabase(databasePath);
        return db.GetCollection<QueuedRequest>("queue").FindAll().ToList();
    }

    public virtual void Remove(Guid requestId)
    {
        using var db = new LiteDatabase(databasePath);
        db.GetCollection<QueuedRequest>("queue").DeleteMany(x => x.RequestId == requestId);
    }
}