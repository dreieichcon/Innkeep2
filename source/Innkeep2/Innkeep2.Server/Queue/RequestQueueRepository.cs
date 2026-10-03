using LiteDB;

namespace Innkeep2.Server.Queue;

public class RequestQueueRepository(string databasePath)
{
    public virtual void Enqueue(QueuedRequest request)
    {
        using var db = new LiteDatabase(databasePath);
        var collection = db.GetCollection<QueuedRequest>("queue");

        if (collection.Exists(x => x.RequestId == request.RequestId && x.Type == request.Type))
            return;

        collection.Insert(request);
    }

    public virtual IReadOnlyList<QueuedRequest> GetAll()
    {
        using var db = new LiteDatabase(databasePath);
        return db.GetCollection<QueuedRequest>("queue").FindAll().OrderBy(x => x.EnqueuedAt).ToList();
    }

    public virtual QueuedRequest? Get(Guid id)
    {
        using var db = new LiteDatabase(databasePath);
        return db.GetCollection<QueuedRequest>("queue").FindById(id);
    }

    public virtual void Update(QueuedRequest request)
    {
        using var db = new LiteDatabase(databasePath);
        db.GetCollection<QueuedRequest>("queue").Update(request);
    }

    public virtual void Remove(Guid id)
    {
        using var db = new LiteDatabase(databasePath);
        db.GetCollection<QueuedRequest>("queue").Delete(id);
    }
}