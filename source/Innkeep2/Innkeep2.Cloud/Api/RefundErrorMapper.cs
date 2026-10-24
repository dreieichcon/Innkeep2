using Demolite.Core.Models;

namespace Innkeep2.Cloud.Api;

public static class RefundErrorMapper
{
    private const string AlreadyRefundedCode = "Refund.AlreadyRefunded";
    private const string CardErrorPrefix = "Refund.Card";

    /// <summary>
    /// Picks the HTTP status code for a failed storno. A refused card refund is a 422, an order that was refunded already is a 409.
    /// </summary>
    public static int StatusCodeFor(Error error)
        => error.Code switch
        {
            AlreadyRefundedCode => StatusCodes.Status409Conflict,
            _ when error.Code.StartsWith(CardErrorPrefix) => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status400BadRequest
        };
}
