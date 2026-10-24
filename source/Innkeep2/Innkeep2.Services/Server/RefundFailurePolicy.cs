using System.Text.Json;
using Demolite.Core.Models;
using Innkeep2.Models.Shared;

namespace Innkeep2.Services.Server;

public enum RefundFailureAction
{
    Queue,
    AlreadyRefunded,
    Reject
}

public sealed record RefundFailureDecision(RefundFailureAction Action, string? Message = null);

public static class RefundFailurePolicy
{
    public const string AlreadyRefundedMessage = "Bestellung wurde bereits storniert.";
    public const string CardOfflineMessage = "Die Cloud ist nicht erreichbar. Eine Kartenzahlung kann nur online storniert werden.";
    public const string CardFailedMessage = "Die Kartenzahlung konnte nicht storniert werden.";

    /// <summary>
    /// Decides what happens with a storno the Cloud did not accept.
    /// Cash stornos are queued for later. Card stornos are never queued, because the money must be refunded online.
    /// </summary>
    public static RefundFailureDecision Decide(PaymentType originalPaymentType, Error error)
    {
        if (error.Code == "Http.409")
            return new RefundFailureDecision(RefundFailureAction.AlreadyRefunded, AlreadyRefundedMessage);

        if (originalPaymentType != PaymentType.NonCash)
            return new RefundFailureDecision(RefundFailureAction.Queue);

        return new RefundFailureDecision(RefundFailureAction.Reject, DescribeCardFailure(error));
    }

    private static string DescribeCardFailure(Error error)
    {
        if (error.Code is "Http.RequestFailed" or "Http.Timeout" or "Http.502" or "Http.503" or "Http.504")
            return CardOfflineMessage;

        return ReadCloudMessage(error.Message) ?? CardFailedMessage;
    }

    private static string? ReadCloudMessage(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);

            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return null;

            string? code = null;
            string? message = null;

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (property.Value.ValueKind != JsonValueKind.String)
                    continue;

                if (string.Equals(property.Name, "code", StringComparison.OrdinalIgnoreCase))
                    code = property.Value.GetString();
                else if (string.Equals(property.Name, "message", StringComparison.OrdinalIgnoreCase))
                    message = property.Value.GetString();
            }

            return code is not null && code.StartsWith("Refund.") && !string.IsNullOrWhiteSpace(message)
                ? message
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
