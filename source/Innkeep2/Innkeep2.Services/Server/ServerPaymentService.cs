using System.Text.Json;
using Demolite.Core.Models;
using Innkeep2.Models.Internal;
using Innkeep2.Requests.Cloud;

namespace Innkeep2.Services.Server;

public sealed class ServerPaymentService(ICloudPaymentClient cloudClient)
{
    public const string CloudUnreachableCode = "Payment.CloudUnreachable";
    public const string CloudUnauthorizedCode = "Payment.CloudUnauthorized";
    public const string FailedCode = "Payment.Failed";

    /// <summary>
    /// Starts a card payment in the Cloud. Card payments are never queued.
    /// </summary>
    public async Task<Result<PaymentStatusResponse>> StartAsync(OrderRequest request, CancellationToken ct = default)
        => Map(await cloudClient.StartCardPaymentAsync(request, ct));

    /// <summary>
    /// Loads the status of a card payment from the Cloud.
    /// </summary>
    public async Task<Result<PaymentStatusResponse>> GetStatusAsync(Guid requestId, CancellationToken ct = default)
        => Map(await cloudClient.GetCardPaymentAsync(requestId, ct));

    /// <summary>
    /// Asks the Cloud to cancel a card payment.
    /// </summary>
    public async Task<Result<PaymentStatusResponse>> CancelAsync(Guid requestId, CancellationToken ct = default)
        => Map(await cloudClient.CancelCardPaymentAsync(requestId, ct));

    /// <summary>
    /// Picks the HTTP status code the Server answers with for a payment error.
    /// </summary>
    public static int StatusCodeFor(Error error)
        => error.Code switch
        {
            "Payment.NotFound" => 404,
            CloudUnreachableCode => 503,
            CloudUnauthorizedCode => 502,
            _ => 400
        };

    private static Result<PaymentStatusResponse> Map(Result<PaymentStatusResponse> result)
        => result.IsSuccess
            ? result
            : Result<PaymentStatusResponse>.Failure(MapError(result.Error!));

    private static Error MapError(Error error)
        => error.Code switch
        {
            "Http.RequestFailed" or "Http.Timeout" or "Http.502" or "Http.503" or "Http.504"
                => new Error(CloudUnreachableCode, "Die Cloud ist nicht erreichbar, Kartenzahlung ist nicht möglich"),
            "Http.401" or "Http.403"
                => new Error(CloudUnauthorizedCode, "Die Cloud lehnt den API Key des Servers ab"),
            "Http.400" or "Http.404"
                => ParseCloudError(error.Message) ?? Failed(),
            _ => Failed()
        };

    private static Error? ParseCloudError(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);

            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return null;

            var code = ReadString(document.RootElement, "code");
            var message = ReadString(document.RootElement, "message");

            return code is not null && code.StartsWith("Payment.") && !string.IsNullOrWhiteSpace(message)
                ? new Error(code, message)
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ReadString(JsonElement element, string name)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase) && property.Value.ValueKind == JsonValueKind.String)
                return property.Value.GetString();
        }

        return null;
    }

    private static Error Failed()
        => new(FailedCode, "Die Kartenzahlung konnte nicht bearbeitet werden");
}
