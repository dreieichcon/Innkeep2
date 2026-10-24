namespace Innkeep2.Cloud.Services.Payments;

public sealed class CardPaymentOptions
{
    /// <summary>
    /// Time after which a pending payment is terminated on the reader. SumUp still has to confirm the outcome.
    /// </summary>
    public TimeSpan OverallTimeout { get; init; } = TimeSpan.FromSeconds(150);

    /// <summary>
    /// Time after the start from which a status request may ask SumUp for the outcome.
    /// </summary>
    public TimeSpan FirstVerificationDelay { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Minimum time between two outcome requests to SumUp for the same payment.
    /// </summary>
    public TimeSpan VerificationInterval { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Time during which a pending payment counts as occupying the reader.
    /// </summary>
    public TimeSpan ReaderBusyWindow { get; init; } = TimeSpan.FromSeconds(180);
}
