using System.Net.Http.Headers;
using System.Text;
using Innkeep2.Models.Core;

namespace Innkeep2.Server.Services;

public sealed class ImageFetchService(HttpClient httpClient)
{
    public const long MaxImageBytes = 10 * 1024 * 1024;

    private const int SniffBytes = 256;

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    // Many image hosts reject requests without a browser like user agent.
    private const string UserAgent = "Mozilla/5.0 (compatible; Innkeep2)";

    public async Task<Result<byte[]>> FetchAsync(string url, CancellationToken ct = default)
    {
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            return Failure("Image.InvalidUrl", "Bitte einen gültigen http(s)-Link angeben.");

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(Timeout);

        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.UserAgent.ParseAdd(UserAgent);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("image/*"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*", 0.5));

        try
        {
            // Headers only: the body is not downloaded until the response has been vetted.
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);

            if (!response.IsSuccessStatusCode)
                return Failure("Image.Download", $"Der Server antwortete mit {(int)response.StatusCode}.");

            if (!IsPlausibleImageType(response.Content.Headers.ContentType?.MediaType))
                return NotAnImage();

            if (response.Content.Headers.ContentLength > MaxImageBytes)
                return TooLarge();

            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var buffer = new MemoryStream();
            var chunk = new byte[81920];
            var sniffed = false;
            int read;

            while ((read = await stream.ReadAsync(chunk, timeout.Token)) > 0)
            {
                buffer.Write(chunk, 0, read);

                if (buffer.Length > MaxImageBytes)
                    return TooLarge();

                if (!sniffed && buffer.Length >= SniffBytes)
                {
                    sniffed = true;

                    if (LooksLikeWebPage(buffer))
                        return NotAnImage();
                }
            }

            if (!sniffed && LooksLikeWebPage(buffer))
                return NotAnImage();

            return Result<byte[]>.Success(buffer.ToArray());
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return Failure("Image.Timeout", "Der Download hat zu lange gedauert.");
        }
        catch (HttpRequestException ex)
        {
            return Failure("Image.Download", $"Download fehlgeschlagen: {ex.Message}");
        }
    }

    /// <summary>
    /// Some servers label images as octet-stream or leave the type out, so only types that are
    /// clearly something else (web pages, JSON, PDFs, ...) are turned away. The decoder has the final say.
    /// </summary>
    private static bool IsPlausibleImageType(string? mediaType)
        => mediaType is null
           || mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
           || mediaType.EndsWith("octet-stream", StringComparison.OrdinalIgnoreCase);

    private static bool LooksLikeWebPage(MemoryStream buffer)
    {
        var head = Encoding.UTF8.GetString(buffer.GetBuffer(), 0, (int)Math.Min(buffer.Length, SniffBytes)).TrimStart();

        return head.StartsWith("<!doctype html", StringComparison.OrdinalIgnoreCase)
               || head.StartsWith("<html", StringComparison.OrdinalIgnoreCase);
    }

    private static Result<byte[]> NotAnImage()
        => Failure("Image.NotAnImage", "Der Link zeigt eine Webseite oder kein Bild. Bitte den direkten Link zur Bilddatei verwenden.");

    private static Result<byte[]> TooLarge()
        => Failure("Image.TooLarge", "Das Bild ist zu groß (max. 10 MB).");

    private static Result<byte[]> Failure(string code, string message)
        => Result<byte[]>.Failure(new Error(code, message));
}
