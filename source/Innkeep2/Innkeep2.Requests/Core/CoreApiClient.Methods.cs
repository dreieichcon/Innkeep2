using Innkeep2.Models.Core;

namespace Innkeep2.Requests.Core;

public abstract partial class CoreApiClient
{
	protected Task<Result<T>> GetAsync<T>(string requestUri, CancellationToken ct = default)
		=> SendAs<T>(new HttpRequestMessage(HttpMethod.Get, requestUri), ct);

	protected Task<Result<T>> PostAsync<T>(string requestUri, object? body = null, CancellationToken ct = default)
		=> SendWithBody<T>(HttpMethod.Post, requestUri, body, ct);

	protected Task<Result<T>> PutAsync<T>(string requestUri, object? body = null, CancellationToken ct = default)
		=> SendWithBody<T>(HttpMethod.Put, requestUri, body, ct);

	protected Task<Result<T>> PatchAsync<T>(string requestUri, object? body = null, CancellationToken ct = default)
		=> SendWithBody<T>(HttpMethod.Patch, requestUri, body, ct);

	protected Task<Result<T>> DeleteAsync<T>(string requestUri, CancellationToken ct = default)
		=> SendAs<T>(new HttpRequestMessage(HttpMethod.Delete, requestUri), ct);
}