using System.Net;
using System.Net.Http.Headers;

namespace Innkeep2.Requests.Fiskaly.Auth;

public sealed class FiskalyAuthHandler(FiskalyTokenProvider tokenProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var tokenResult = await tokenProvider.GetTokenAsync(ct);

        if (!tokenResult.IsSuccess)
            return new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent(tokenResult.Error!.Message),
                RequestMessage = request
            };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Value);

        return await base.SendAsync(request, ct);
    }
}