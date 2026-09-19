using System.Security.Cryptography;
using System.Text;

namespace Innkeep2.Credentials.ApiKeys;

public static class ApiKeyHasher
{
    public static string GenerateKey()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    public static string Hash(string key)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
}