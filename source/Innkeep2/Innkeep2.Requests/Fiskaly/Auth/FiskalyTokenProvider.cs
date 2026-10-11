using Demolite.Core.Models;
using Demolite.Http.Auth;
using Innkeep2.Credentials;
using Innkeep2.Credentials.Models;

namespace Innkeep2.Requests.Fiskaly.Auth;

public sealed class FiskalyTokenProvider(
    FiskalyAuthClient authClient,
    ActiveCredentialsProvider<FiskalyCredential> credentials
) : CachedTokenProvider
{
    protected override async Task<Result<AccessToken>> RequestTokenAsync(CancellationToken ct)
    {
        var active = credentials.GetActive();
        var result = await authClient.AuthenticateAsync(active.ApiKey, active.ApiSecret, ct);

        return result.IsSuccess
            ? Result<AccessToken>.Success(
                new AccessToken(result.Value!.AccessToken, TimeSpan.FromSeconds(result.Value.AccessTokenExpiresIn)))
            : Result<AccessToken>.Failure(result.Error!);
    }
}
