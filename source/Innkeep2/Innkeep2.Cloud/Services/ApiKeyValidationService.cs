using System.Security.Cryptography;
using System.Text;
using Innkeep2.Cloud.AppDb.Models;
using Innkeep2.Cloud.AppDb.Repositories;
using Innkeep2.Credentials.ApiKeys;
using Innkeep2.Database.Model;
using Innkeep2.Models.Core;

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
            Operation = Operation.Create,
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