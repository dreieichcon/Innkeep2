using Innkeep2.Cloud.TransactionDb.Models;
using Innkeep2.Models.Shared;

namespace Innkeep2.Cloud.Services.Payments;

public enum SumUpOutcome
{
    Pending,
    Succeeded,
    Failed,
    Unknown
}

public static class CardPaymentRules
{
    /// <summary>
    /// Maps a SumUp transaction status to the outcome of the payment.
    /// </summary>
    public static SumUpOutcome MapOutcome(string? sumUpStatus)
        => sumUpStatus?.ToUpperInvariant() switch
        {
            "SUCCESSFUL" => SumUpOutcome.Succeeded,
            "FAILED" or "CANCELLED" => SumUpOutcome.Failed,
            "PENDING" => SumUpOutcome.Pending,
            _ => SumUpOutcome.Unknown
        };

    /// <summary>
    /// Picks the final status once SumUp has confirmed that the payment failed.
    /// </summary>
    public static PaymentRequestStatus ResolveFailedStatus(PaymentTerminationReason? terminationReason)
        => terminationReason switch
        {
            PaymentTerminationReason.UserCancelled => PaymentRequestStatus.Cancelled,
            PaymentTerminationReason.Timeout => PaymentRequestStatus.Expired,
            _ => PaymentRequestStatus.Failed
        };

    public static bool IsFinal(PaymentRequestStatus status)
        => status is PaymentRequestStatus.Completed
            or PaymentRequestStatus.Failed
            or PaymentRequestStatus.Cancelled
            or PaymentRequestStatus.Expired;

    /// <summary>
    /// Allows only the transitions Pending to Paid, Failed, Cancelled or Expired, and Paid to Completed.
    /// </summary>
    public static bool CanTransition(PaymentRequestStatus from, PaymentRequestStatus to)
        => from switch
        {
            PaymentRequestStatus.Pending => to is PaymentRequestStatus.Paid
                or PaymentRequestStatus.Failed
                or PaymentRequestStatus.Cancelled
                or PaymentRequestStatus.Expired,
            PaymentRequestStatus.Paid => to is PaymentRequestStatus.Completed,
            _ => false
        };

    /// <summary>
    /// A status request asks SumUp for the outcome after the first delay and then at most once per interval.
    /// </summary>
    public static bool ShouldVerify(PaymentRequest request, DateTime nowUtc, DateTime? lastVerifiedUtc, CardPaymentOptions options)
        => request.Status == PaymentRequestStatus.Pending
           && nowUtc - request.CreatedAt >= options.FirstVerificationDelay
           && (lastVerifiedUtc is null || nowUtc - lastVerifiedUtc.Value >= options.VerificationInterval);

    /// <summary>
    /// A pending payment must be terminated on the reader once, after the overall timeout.
    /// </summary>
    public static bool ShouldTerminateForTimeout(PaymentRequest request, DateTime nowUtc, CardPaymentOptions options)
        => request.Status == PaymentRequestStatus.Pending
           && request.TerminationReason is null
           && nowUtc - request.CreatedAt >= options.OverallTimeout;

    /// <summary>
    /// Earliest creation time of a pending payment that still occupies the reader.
    /// </summary>
    public static DateTime ReaderBusyCutoff(DateTime nowUtc, CardPaymentOptions options)
        => nowUtc - options.ReaderBusyWindow;
}
