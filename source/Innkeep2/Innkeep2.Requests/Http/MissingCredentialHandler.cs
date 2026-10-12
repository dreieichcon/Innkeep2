using System.Net;

namespace Innkeep2.Requests.Http;

public sealed class MissingCredentialHandler(Func<bool> hasCredential) : DelegatingHandler
{
    public static readonly Uri PlaceholderAddress = new("http://credentials-not-configured.invalid/");

    /// <summary>
    /// Answers with a service unavailable response while no credential is active, without contacting the network.
    /// </summary>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (hasCredential())
            return base.SendAsync(request, ct);

        var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        {
            RequestMessage = request,
            Content = new StringContent("No active credential configured.")
        };

        return Task.FromResult(response);
    }
}
