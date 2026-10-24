using Innkeep2.Requests.Server;

namespace Innkeep2.Services.Client;

public sealed class ClientCardPaymentService(
    IServerPaymentClient client,
    ClientCartService cart,
    ICompletedOrderRegistry completedOrders,
    ClientCardPaymentOptions options,
    TimeProvider timeProvider)
{
    public Func<TimeSpan, CancellationToken, Task> Delay { get; init; } = (time, ct) => Task.Delay(time, ct);

    /// <summary>
    /// Creates the state for one run of the card payment dialog.
    /// </summary>
    public CardPaymentSession CreateSession()
        => new(client, cart, completedOrders, options, timeProvider, Delay);

    /// <summary>
    /// A card payment is possible when the cart holds an amount above zero.
    /// </summary>
    public bool CanPayByCard => cart.Items.Count > 0 && cart.Total > 0;
}
