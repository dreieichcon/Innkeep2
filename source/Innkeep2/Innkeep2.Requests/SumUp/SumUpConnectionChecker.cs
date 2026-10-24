using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Innkeep2.Models.SumUp;
using Microsoft.Extensions.DependencyInjection;

namespace Innkeep2.Requests.SumUp;

public enum SumUpConnectionState
{
    Incomplete,
    InvalidKey,
    MerchantNotFound,
    ReaderNotFound,
    ReaderNotPaired,
    Unreachable,
    Connected
}

public sealed record SumUpConnectionCheckResult(
    SumUpConnectionState State,
    string Message,
    string? ReaderName = null,
    string? DeviceModel = null,
    bool? ReaderOnline = null,
    string? ReaderState = null
);

public sealed class SumUpConnectionChecker(HttpClient httpClient, [FromKeyedServices("sumup")] JsonSerializerOptions serializerOptions)
    : ISumUpConnectionChecker
{
    private const string PairedStatus = "paired";
    private const string OnlineStatus = "ONLINE";

    /// <summary>
    /// Checks API key and merchant code by listing the readers of the merchant, without using the active credential.
    /// </summary>
    public async Task<SumUpConnectionCheckResult> CheckAccountAsync(string? apiKey, string? merchantCode, CancellationToken ct = default)
        => string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(merchantCode)
            ? Failure(SumUpConnectionState.Incomplete, "Bitte API Key und Händlercode angeben")
            : await CheckAccountSafelyAsync(apiKey, merchantCode, ct);

    private async Task<SumUpConnectionCheckResult> CheckAccountSafelyAsync(string apiKey, string merchantCode, CancellationToken ct)
    {
        try
        {
            return await CheckAccountCoreAsync(apiKey, merchantCode, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            return Failure(SumUpConnectionState.Unreachable, "SumUp nicht erreichbar");
        }
    }

    private async Task<SumUpConnectionCheckResult> CheckAccountCoreAsync(string apiKey, string merchantCode, CancellationToken ct)
    {
        using var response = await GetAsync($"v0.1/merchants/{Uri.EscapeDataString(merchantCode)}/readers", apiKey, ct);

        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
                => Failure(SumUpConnectionState.InvalidKey, "API Key ungültig oder Berechtigung readers.read fehlt"),
            HttpStatusCode.NotFound
                => Failure(SumUpConnectionState.MerchantNotFound, "Händlercode nicht gefunden"),
            _ when !response.IsSuccessStatusCode
                => Failure(SumUpConnectionState.Unreachable, $"SumUp antwortete mit Status {(int)response.StatusCode}"),
            _ => BuildAccountResult(await response.Content.ReadFromJsonAsync<SumUpReaderList>(serializerOptions, ct))
        };
    }

    private static SumUpConnectionCheckResult BuildAccountResult(SumUpReaderList? readers)
        => readers switch
        {
            null => Failure(SumUpConnectionState.Unreachable, "SumUp lieferte eine ungültige Antwort"),
            _ => new SumUpConnectionCheckResult(SumUpConnectionState.Connected, DescribePairedReaders(readers.Items.Count(x => IsPaired(x))))
        };

    private static string DescribePairedReaders(int pairedCount)
        => pairedCount switch
        {
            0 => "Verbunden, es ist noch kein Reader gekoppelt",
            1 => "Verbunden, 1 Reader gekoppelt",
            _ => $"Verbunden, {pairedCount} Reader gekoppelt"
        };

    private static bool IsPaired(SumUpReader reader)
        => string.Equals(reader.Status, PairedStatus, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Checks the given reader credentials against SumUp without using the active credential.
    /// </summary>
    public async Task<SumUpConnectionCheckResult> CheckReaderAsync(string? apiKey, string? merchantCode, string? readerId, CancellationToken ct = default)
        => string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(merchantCode) || string.IsNullOrWhiteSpace(readerId)
            ? Failure(SumUpConnectionState.Incomplete, "Bitte API Key, Händlercode und Reader-ID angeben")
            : await CheckReaderSafelyAsync(apiKey, merchantCode, readerId, ct);

    private async Task<SumUpConnectionCheckResult> CheckReaderSafelyAsync(string apiKey, string merchantCode, string readerId, CancellationToken ct)
    {
        try
        {
            return await CheckReaderCoreAsync(apiKey, merchantCode, readerId, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            return Failure(SumUpConnectionState.Unreachable, "SumUp nicht erreichbar");
        }
    }

    private async Task<SumUpConnectionCheckResult> CheckReaderCoreAsync(string apiKey, string merchantCode, string readerId, CancellationToken ct)
    {
        var readerUrl = $"v0.1/merchants/{Uri.EscapeDataString(merchantCode)}/readers/{Uri.EscapeDataString(readerId)}";

        using var response = await GetAsync(readerUrl, apiKey, ct);

        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
                => Failure(SumUpConnectionState.InvalidKey, "API Key ungültig oder Berechtigung readers.read fehlt"),
            HttpStatusCode.NotFound
                => Failure(SumUpConnectionState.ReaderNotFound, "Händlercode oder Reader-ID nicht gefunden"),
            _ when !response.IsSuccessStatusCode
                => Failure(SumUpConnectionState.Unreachable, $"SumUp antwortete mit Status {(int)response.StatusCode}"),
            _ => await EvaluateReaderAsync(await response.Content.ReadFromJsonAsync<SumUpReader>(serializerOptions, ct), readerUrl, apiKey, ct)
        };
    }

    private async Task<SumUpConnectionCheckResult> EvaluateReaderAsync(SumUpReader? reader, string readerUrl, string apiKey, CancellationToken ct)
        => reader switch
        {
            null
                => Failure(SumUpConnectionState.Unreachable, "SumUp lieferte eine ungültige Antwort"),
            _ when !IsPaired(reader)
                => Failure(SumUpConnectionState.ReaderNotPaired, $"Reader hat den Status '{reader.Status}', er muss 'paired' sein", reader),
            _ => BuildConnected(reader, await TryGetStatusAsync($"{readerUrl}/status", apiKey, ct))
        };

    private static SumUpConnectionCheckResult Failure(SumUpConnectionState state, string message, SumUpReader? reader = null)
        => new(state, message, reader?.Name, reader?.Device?.Model);

    private async Task<HttpResponseMessage> GetAsync(string url, string apiKey, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        return await httpClient.SendAsync(request, ct);
    }

    private async Task<SumUpReaderStatus?> TryGetStatusAsync(string url, string apiKey, CancellationToken ct)
    {
        using var response = await GetAsync(url, apiKey, ct);

        if (!response.IsSuccessStatusCode)
            return null;

        try
        {
            return (await response.Content.ReadFromJsonAsync<SumUpReaderStatusResponse>(serializerOptions, ct))?.Data;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static SumUpConnectionCheckResult BuildConnected(SumUpReader reader, SumUpReaderStatus? status)
    {
        bool? online = status is null ? null : string.Equals(status.Status, OnlineStatus, StringComparison.OrdinalIgnoreCase);
        var onlineText = online switch
        {
            true => "online",
            false => "offline",
            _ => "Online-Status unbekannt"
        };
        var model = reader.Device?.Model ?? "unbekannt";

        return new SumUpConnectionCheckResult(
            SumUpConnectionState.Connected,
            $"Verbunden mit '{reader.Name}' ({model}), {onlineText}",
            reader.Name,
            reader.Device?.Model,
            online,
            status?.State);
    }
}
