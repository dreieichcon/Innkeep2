using Demolite.Core.Models;

namespace Innkeep2.CredentialsDb.Interfaces;

public interface ILiteDbRepository<T> where T : ILiteDbItem
{
    Task<Result<IReadOnlyList<T>>> GetAllAsync();
    Task<Result<T>> GetByIdAsync(Guid id);
    Task<Result<T>> CreateAsync(T item);
    Task<Result<T>> UpdateAsync(T item);
    Task<Result> DeleteAsync(Guid id);
}
