using Innkeep2.Credentials.Models;

namespace Innkeep2.Requests.Cloud.Auth;

public sealed class CloudAuthHandler(CloudCredential credential) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        request.RequestUri = new Uri(new Uri(credential.CloudUrl), request.RequestUri!);
        request.Headers.Add("X-Api-Key", credential.ApiKey);

        return base.SendAsync(request, ct);
    }
}