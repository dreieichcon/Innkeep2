using System.Net;
using System.Text;

namespace Innkeep2.Requests.Tests.SumUp;

public sealed record CapturedRequest(HttpMethod Method, Uri Uri, string? Authorization, string? ContentType, string? Body);

public sealed class StubHttpMessageHandler : HttpMessageHandler
{
	private readonly Queue<Func<HttpResponseMessage>> _responses = [];

	public List<CapturedRequest> Requests { get; } = [];

	public void Enqueue(HttpStatusCode statusCode, string? body = null, string mediaType = "application/json")
	{
		var response = new HttpResponseMessage(statusCode);

		if (body is not null)
			response.Content = new StringContent(body, Encoding.UTF8, mediaType);

		_responses.Enqueue(() => response);
	}

	public void EnqueueException(Exception exception)
		=> _responses.Enqueue(() => throw exception);

	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

		Requests.Add(new CapturedRequest(
			request.Method,
			request.RequestUri!,
			request.Headers.Authorization?.ToString(),
			request.Content?.Headers.ContentType?.MediaType,
			body));

		if (_responses.Count == 0)
			throw new InvalidOperationException("No response was queued for the request.");

		return _responses.Dequeue()();
	}
}
