using Demolite.Core.Models;
using Innkeep2.CredentialsDb.Interfaces;
using LiteDB;

namespace Innkeep2.CredentialsDb.Repositories;

public abstract class LiteDbRepository<T>(string databasePath, string collectionName) : ILiteDbRepository<T>
    where T : ILiteDbItem
{
    /// <summary>
    /// Loads all items of the collection.
    /// </summary>
    public Task<Result<IReadOnlyList<T>>> GetAllAsync()
    {
        var items = Execute(collection => collection.FindAll().ToList());
        return Task.FromResult(Result<IReadOnlyList<T>>.Success(items));
    }

    /// <summary>
    /// Loads a single item by its id.
    /// </summary>
    public Task<Result<T>> GetByIdAsync(Guid id)
    {
        var item = Execute(collection => collection.FindById(id));
        return Task.FromResult(item is null ? NotFound(id) : Result<T>.Success(item));
    }

    /// <summary>
    /// Inserts a new item.
    /// </summary>
    public virtual Task<Result<T>> CreateAsync(T item)
    {
        Execute(collection => collection.Insert(item));
        return Task.FromResult(Result<T>.Success(item));
    }

    /// <summary>
    /// Updates an existing item.
    /// </summary>
    public virtual Task<Result<T>> UpdateAsync(T item)
    {
        var updated = Execute(collection => collection.Update(item));
        return Task.FromResult(updated ? Result<T>.Success(item) : NotFound(item.Id));
    }

    /// <summary>
    /// Deletes an item by its id.
    /// </summary>
    public Task<Result> DeleteAsync(Guid id)
    {
        var deleted = Execute(collection => collection.Delete(id));

        return Task.FromResult(deleted
            ? Result.Success()
            : Result.Failure(NotFoundError(id)));
    }

    protected TResult Execute<TResult>(Func<ILiteCollection<T>, TResult> action)
    {
        using var db = new LiteDatabase(databasePath);
        return action(db.GetCollection<T>(collectionName));
    }

    protected TResult ExecuteInTransaction<TResult>(Func<ILiteCollection<T>, TResult> action)
    {
        using var db = new LiteDatabase(databasePath);
        db.BeginTrans();

        var result = action(db.GetCollection<T>(collectionName));

        db.Commit();
        return result;
    }

    protected static Result<T> NotFound(Guid id) => Result<T>.Failure(NotFoundError(id));

    protected static Error NotFoundError(Guid id)
        => new("Db.NotFound", $"{typeof(T).Name} with id '{id}' not found.");
}
