namespace Innkeep2.Cloud.Services.Payments;

public sealed class SumUpPairingOptions
{
    /// <summary>
    /// Time between two checks whether the device has confirmed the pairing.
    /// </summary>
    public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Time to wait for the confirmation on the device before giving up.
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(60);

    public int MaxPolls => PollInterval > TimeSpan.Zero
        ? Math.Max(1, (int)Math.Ceiling(Timeout / PollInterval))
        : 1;
}
