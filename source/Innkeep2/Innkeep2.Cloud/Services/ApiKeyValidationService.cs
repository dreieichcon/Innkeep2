using System.Security.Cryptography;
using System.Text;
using Demolite.Core.Models;
using Innkeep2.Cloud.AppDb.Models;
using Innkeep2.Cloud.AppDb.Repositories;
using Innkeep2.Security.ApiKeys;

namespace Innkeep2.Cloud.Services;

public class ApiKeyValidationService(InnkeepCloudApiKeyRepository repository)
{
    public async Task<Result<string>> GenerateApiKey(string name)
    {
        var key = ApiKeyHasher.GenerateKey();

        var model = new InnkeepCloudApiKey()
        {
            Name = name,
            KeyHash = ApiKeyHasher.Hash(key),
            CreatedAt = DateTime.UtcNow,
        };

        var result = await repository.CreateAsync(model);

        return result.IsSuccess ? Result<string>.Success(key) : Result<string>.Failure(result.Error!);
    }

    public async Task<bool> ValidateApiKey(string key)
    {
        var hash = ApiKeyHasher.Hash(key);

        var results = await repository
            .GetAllCustomAsync(x => !x.Revoked && x.KeyHash == hash);

        return results.Value?.Count > 0;
    }
}
