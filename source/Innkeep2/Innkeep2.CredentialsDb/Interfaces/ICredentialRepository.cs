using Demolite.Core.Models;
using Innkeep2.CredentialsDb.Models;

namespace Innkeep2.CredentialsDb.Interfaces;

public interface ICredentialRepository<T> : ILiteDbRepository<T> where T : ICredentialDocument
{
    Task<Result<T>> GetActiveAsync();
    Task<Result> SetActiveAsync(Guid id);
}
