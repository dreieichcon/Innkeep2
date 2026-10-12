using Demolite.Core.Models;
using Innkeep2.CredentialsDb.Interfaces;
using Innkeep2.CredentialsDb.Models;
using LiteDB;

namespace Innkeep2.CredentialsDb.Repositories;

public abstract class CredentialRepository<T>(string databasePath, string collectionName)
    : LiteDbRepository<T>(databasePath, collectionName), ICredentialRepository<T>
    where T : ICredentialDocument
{
    /// <summary>
    /// Inserts a new item. The first item becomes active, an active item deactivates all others.
    /// </summary>
    public override Task<Result<T>> CreateAsync(T item)
    {
        ExecuteInTransaction(collection =>
        {
            if (collection.Count() == 0)
                item.Active = true;

            if (item.Active)
                DeactivateOthers(collection, item.Id);

            return collection.Insert(item);
        });

        return Task.FromResult(Result<T>.Success(item));
    }

    /// <summary>
    /// Updates an existing item. An active item deactivates all others.
    /// </summary>
    public override Task<Result<T>> UpdateAsync(T item)
    {
        var updated = ExecuteInTransaction(collection =>
        {
            if (item.Active)
                DeactivateOthers(collection, item.Id);

            return collection.Update(item);
        });

        return Task.FromResult(updated ? Result<T>.Success(item) : NotFound(item.Id));
    }

    /// <summary>
    /// Loads the active item.
    /// </summary>
    public Task<Result<T>> GetActiveAsync()
    {
        var item = Execute(collection => collection.FindOne(x => x.Active));

        return Task.FromResult(item is null
            ? Result<T>.Failure(new Error("Credentials.NoActive", $"No active {typeof(T).Name} found."))
            : Result<T>.Success(item));
    }

    /// <summary>
    /// Marks the item as active and deactivates all others.
    /// </summary>
    public Task<Result> SetActiveAsync(Guid id)
    {
        var activated = ExecuteInTransaction(collection =>
        {
            var item = collection.FindById(id);

            if (item is null)
                return false;

            DeactivateOthers(collection, id);
            item.Active = true;
            return collection.Update(item);
        });

        return Task.FromResult(activated ? Result.Success() : Result.Failure(NotFoundError(id)));
    }

    private static void DeactivateOthers(ILiteCollection<T> collection, Guid exceptId)
    {
        foreach (var other in collection.Find(x => x.Active && x.Id != exceptId).ToList())
        {
            other.Active = false;
            collection.Update(other);
        }
    }
}
