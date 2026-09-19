using Innkeep2.Credentials.ApiKeys;
using Innkeep2.Models.Core;
using LiteDB;

namespace Innkeep2.Server.Security;

public sealed class ApiKeyRepository(string databasePath)
{
    public void Add(ApiKeyEntry entry)
    {
        using var db = new LiteDatabase(databasePath);
        db.GetCollection<ApiKeyEntry>("apikeys").Insert(entry);
    }

    public Task<Result<IReadOnlyList<ApiKeyEntry>>> GetAllAsync()
    {
        using var db = new LiteDatabase(databasePath);
        var entries = db.GetCollection<ApiKeyEntry>("apikeys").FindAll().ToList();
        return Task.FromResult(Result<IReadOnlyList<ApiKeyEntry>>.Success(entries));
    }

    public Task<Result<ApiKeyEntry>> UpdateAsync(ApiKeyEntry entry)
    {
        using var db = new LiteDatabase(databasePath);
        var updated = db.GetCollection<ApiKeyEntry>("apikeys").Update(entry);

        return Task.FromResult(updated
            ? Result<ApiKeyEntry>.Success(entry)
            : Result<ApiKeyEntry>.Failure(new Error("Db.NotFound", $"ApiKeyEntry with id '{entry.Id}' not found.")));
    }

    public Result<string> GenerateApiKey(string name)
    {
        var key = ApiKeyHasher.GenerateKey();

        Add(new ApiKeyEntry
        {
            Name = name,
            KeyHash = ApiKeyHasher.Hash(key),
            CreatedAt = DateTime.UtcNow
        });

        return Result<string>.Success(key);
    }

    public bool ValidateApiKey(string key)
    {
        var hash = ApiKeyHasher.Hash(key);

        using var db = new LiteDatabase(databasePath);
        return db.GetCollection<ApiKeyEntry>("apikeys").Exists(x => !x.Revoked && x.KeyHash == hash);
    }
}