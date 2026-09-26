using Innkeep2.Credentials.Models;

namespace Innkeep2.Requests.Server.Auth;

public sealed class ServerAuthHandler(ServerCredential credential) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        request.Headers.Add("X-Api-Key", credential.ApiKey);
        return base.SendAsync(request, ct);
    }
}