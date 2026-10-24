using Demolite.Core.Models;
using Innkeep2.Models.Internal;
using Innkeep2.Services.Server;

namespace Innkeep2.Server.Api;

public static class PaymentEndpoints
{
    /// <summary>
    /// Maps the card payment endpoints for Client. They pass the calls on to the Cloud.
    /// </summary>
    public static void MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/payments/card/start", async (OrderRequest request, ServerPaymentService service, CancellationToken ct)
            => ToResult(await service.StartAsync(request, ct)));

        app.MapGet("/payments/card/{requestId:guid}", async (Guid requestId, ServerPaymentService service, CancellationToken ct)
            => ToResult(await service.GetStatusAsync(requestId, ct)));

        app.MapPost("/payments/card/{requestId:guid}/cancel", async (Guid requestId, ServerPaymentService service, CancellationToken ct)
            => ToResult(await service.CancelAsync(requestId, ct)));
    }

    private static IResult ToResult(Result<PaymentStatusResponse> result)
        => result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Json(result.Error, statusCode: ServerPaymentService.StatusCodeFor(result.Error!));
}
