using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Innkeep2.Models.Core;

namespace Innkeep2.Requests.Core;

public abstract partial class CoreApiClient(HttpClient httpClient, JsonSerializerOptions serializerOptions)
{
	protected HttpClient HttpClient { get; } = httpClient;
	
	protected JsonSerializerOptions SerializerOptions { get; } = serializerOptions;
	
	protected async Task<Result<T>> SendAs<T>(HttpRequestMessage request, CancellationToken ct = default)
	{
		var sendResult = await TrySendAsync(request, ct);
		
		if (!sendResult.IsSuccess)
			return Result<T>.Failure(sendResult.Error!);

		using var response = sendResult.Value!;
		var content = await response.Content.ReadAsStringAsync(ct);

		return response.IsSuccessStatusCode
			? BuildSuccessResult<T>(response, content, request)
			: Result<T>.Failure(BuildErrorFromResponse(response, content, request));
	}

	private async Task<Result<HttpResponseMessage>> TrySendAsync(HttpRequestMessage request, CancellationToken ct)
	{
		try
		{
			var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
			return Result<HttpResponseMessage>.Success(response);
		}
		catch (HttpRequestException ex)
		{
			return Result<HttpResponseMessage>.Failure(new Error(
				"Http.RequestFailed",
				$"Request to {request.RequestUri} failed: {ex.Message}",
				ex,
				new Dictionary<string, object?> { ["statusCode"] = (int?)ex.StatusCode }));
		}
		catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
		{
			return Result<HttpResponseMessage>.Failure(new Error(
				"Http.Timeout",
				$"Request to {request.RequestUri} timed out: {ex.Message}",
				ex));
		}
	}
	
	private Result<T> BuildSuccessResult<T>(HttpResponseMessage response, string content, HttpRequestMessage request)
	{
		if (IsEmptyResult<T>(response, content))
			return Result<T>.Success(default!);

		return TryDeserialize<T>(content, request);
	}
	
	private static bool IsEmptyResult<T>(HttpResponseMessage response, string content) =>
		typeof(T) == typeof(Unit)
		|| response.StatusCode == HttpStatusCode.NoContent
		|| content.Length == 0;
	
	private Result<T> TryDeserialize<T>(string content, HttpRequestMessage request)
	{
		try
		{
			var value = JsonSerializer.Deserialize<T>(content, SerializerOptions);

			return value is null
				? Result<T>.Failure(new Error(
					"Http.EmptyBody",
					$"Response body for {request.RequestUri} deserialized to null"))
				: Result<T>.Success(value);
		}
		catch (JsonException ex)
		{
			return Result<T>.Failure(new Error(
				"Http.DeserializationFailed",
				$"Failed to deserialize response from {request.RequestUri}: {ex.Message}",
				ex,
				new Dictionary<string, object?> { ["rawContent"] = content }));
		}
	}
	
	private static Error BuildErrorFromResponse(HttpResponseMessage response, string content, HttpRequestMessage request)
	{
		var message = string.IsNullOrWhiteSpace(content)
			? response.ReasonPhrase ?? "Request failed"
			: content;

		return new Error(
			$"Http.{(int)response.StatusCode}",
			message,
			Metadata: new Dictionary<string, object?>
			{
				["statusCode"] = (int)response.StatusCode,
				["requestUri"] = request.RequestUri?.ToString(),
			});
	}

	private Task<Result<T>> SendWithBody<T>(HttpMethod method, string requestUri, object? body, CancellationToken ct)
	{
		var request = new HttpRequestMessage(method, requestUri);
		if (body is not null)
			request.Content = JsonContent.Create(body, options: SerializerOptions);

		return SendAs<T>(request, ct);
	}
}

public readonly struct Unit;