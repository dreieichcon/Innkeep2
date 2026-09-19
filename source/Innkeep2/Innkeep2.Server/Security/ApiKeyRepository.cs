using LiteDB;

namespace Innkeep2.Server.Security;

public sealed class ApiKeyRepository(string databasePath)
{
    public void Add(ApiKeyEntry entry)
    {
        using var db = new LiteDatabase(databasePath);
        db.GetCollection<ApiKeyEntry>("apikeys").Insert(entry);
    }

    public bool IsValid(string hash)
    {
        using var db = new LiteDatabase(databasePath);
        return db.GetCollection<ApiKeyEntry>("apikeys").Exists(x => !x.Revoked && x.KeyHash == hash);
    }

    public void Revoke(int id)
    {
        using var db = new LiteDatabase(databasePath);
        var collection = db.GetCollection<ApiKeyEntry>("apikeys");
        var entry = collection.FindById(id);

        if (entry is null)
            return;

        entry.Revoked = true;
        collection.Update(entry);
    }
}