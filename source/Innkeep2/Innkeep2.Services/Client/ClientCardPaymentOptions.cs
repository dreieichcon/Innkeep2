namespace Innkeep2.Services.Client;

public sealed class ClientCardPaymentOptions
{
    /// <summary>
    /// Time between two status requests while a payment is pending.
    /// </summary>
    public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(1.5);

    /// <summary>
    /// Time after the start from which a payment without a final status is given up.
    /// Covers the timeout of the Cloud and a short wait for the confirmation of SumUp.
    /// </summary>
    public TimeSpan GiveUpAfter { get; init; } = TimeSpan.FromSeconds(180);
}
