using Innkeep2.CredentialsDb.Interfaces;
using Innkeep2.CredentialsDb.Models;

namespace Innkeep2.CredentialsDb;

public sealed class ActiveCredentialProvider<T>(ICredentialRepository<T> repository)
    where T : class, ICredentialDocument
{
    private volatile T? _active;

    public T? Active => _active;

    public bool HasActive => _active is not null;

    public event EventHandler? Changed;

    /// <summary>
    /// Returns the cached active credential. Throws if none is loaded.
    /// </summary>
    public T GetActive()
        => _active ?? throw new InvalidOperationException($"No active {typeof(T).Name} configured.");

    /// <summary>
    /// Loads the active credential from the repository into the cache and raises the changed event.
    /// </summary>
    public async Task ReloadAsync()
    {
        var result = await repository.GetActiveAsync();
        _active = result.IsSuccess ? result.Value : null;

        Changed?.Invoke(this, EventArgs.Empty);
    }
}
