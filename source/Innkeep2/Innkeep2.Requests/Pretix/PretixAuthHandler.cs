using System.Net.Http.Headers;
using Innkeep2.Credentials;
using Innkeep2.Credentials.Models;

namespace Innkeep2.Requests.Pretix;

public sealed class PretixAuthHandler(ActiveCredentialsProvider<PretixCredential> credentials) : DelegatingHandler
{
	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
	{
		var active = credentials.GetActive();

		request.RequestUri = new Uri(new Uri(active.BaseUrl), request.RequestUri!);
		request.Headers.Authorization = new AuthenticationHeaderValue("Token", active.ApiKey);

		return base.SendAsync(request, ct);
	}
}