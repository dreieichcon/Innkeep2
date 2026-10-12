using Demolite.Core.Models;
using Demolite.Http.Auth;
using Innkeep2.CredentialsDb;
using Innkeep2.CredentialsDb.Models;

namespace Innkeep2.Requests.Fiskaly.Auth;

public sealed class FiskalyTokenProvider : CachedTokenProvider
{
    private readonly FiskalyAuthClient _authClient;
    private readonly ActiveCredentialProvider<CredentialPair> _credentials;

    public FiskalyTokenProvider(FiskalyAuthClient authClient, ActiveCredentialProvider<CredentialPair> credentials)
    {
        _authClient = authClient;
        _credentials = credentials;
        _credentials.Changed += (_, _) => Invalidate();
    }

    protected override async Task<Result<AccessToken>> RequestTokenAsync(CancellationToken ct)
    {
        if (_credentials.Active?.Fiskaly is not { } fiskaly)
            return Result<AccessToken>.Failure(new Error("Credentials.NoActive", "No active Fiskaly credential configured."));

        var result = await _authClient.AuthenticateAsync(fiskaly.ApiKey, fiskaly.ApiSecret, ct);

        return result.IsSuccess
            ? Result<AccessToken>.Success(
                new AccessToken(result.Value!.AccessToken, TimeSpan.FromSeconds(result.Value.AccessTokenExpiresIn)))
            : Result<AccessToken>.Failure(result.Error!);
    }
}
