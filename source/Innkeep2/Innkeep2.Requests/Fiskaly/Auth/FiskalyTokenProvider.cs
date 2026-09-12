using Innkeep2.Credentials;
using Innkeep2.Credentials.Models;
using Innkeep2.Models.Core;

namespace Innkeep2.Requests.Fiskaly.Auth;

public sealed class FiskalyTokenProvider(
    FiskalyAuthClient authClient,
    ActiveCredentialsProvider<FiskalyCredential> credentials
)
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private string? _token;
    private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

    public async Task<Result<string>> GetTokenAsync(CancellationToken ct = default)
    {
        if (_token is not null && DateTimeOffset.UtcNow < _expiresAt)
            return Result<string>.Success(_token);

        await _lock.WaitAsync(ct);

        try
        {
            if (_token is not null && DateTimeOffset.UtcNow < _expiresAt)
                return Result<string>.Success(_token);

            var active = credentials.GetActive();
            var result = await authClient.AuthenticateAsync(active.ApiKey, active.ApiSecret, ct);

            if (!result.IsSuccess)
                return Result<string>.Failure(result.Error!);

            _token = result.Value!.AccessToken;
            _expiresAt = DateTimeOffset.UtcNow.AddSeconds(result.Value!.AccessTokenExpiresIn - 60);

            return Result<string>.Success(_token);
        }
        finally
        {
            _lock.Release();
        }
    }
}