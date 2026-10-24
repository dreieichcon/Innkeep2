using Demolite.Core.Models;
using Innkeep2.Cloud.Services.Payments;
using Innkeep2.Models.Internal;
using Innkeep2.Models.SumUp;
using Serilog;

namespace Innkeep2.Cloud.Api;

public static class PaymentEndpoints
{
    /// <summary>
    /// Maps the card payment endpoints for Server. They belong into the group that checks the API key.
    /// </summary>
    public static void MapCardPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/payments/card/start", async (OrderRequest request, CardPaymentService service, CancellationToken ct)
            => ToResult(await service.StartAsync(request, ct)));

        app.MapGet("/payments/card/{requestId:guid}", async (Guid requestId, CardPaymentService service, CancellationToken ct)
            => ToResult(await service.GetStatusAsync(requestId, ct)));

        app.MapPost("/payments/card/{requestId:guid}/cancel", async (Guid requestId, CardPaymentService service, CancellationToken ct)
            => ToResult(await service.CancelAsync(requestId, ct)));
    }

    /// <summary>
    /// Maps the public SumUp callback. It answers at once and processes the event in the background.
    /// </summary>
    public static void MapSumUpWebhook(this IEndpointRouteBuilder app)
    {
        app.MapPost("/webhooks/sumup/{token}", (string token, SumUpWebhookEvent webhook, CardPaymentService service) =>
            {
                if (!service.IsWebhookTokenValid(token))
                    return Results.Text("Not found", statusCode: StatusCodes.Status404NotFound);

                _ = ProcessInBackgroundAsync(service, token, webhook);

                return Results.Ok();
            })
            .AllowAnonymous();
    }

    private static async Task ProcessInBackgroundAsync(CardPaymentService service, string token, SumUpWebhookEvent webhook)
    {
        try
        {
            await service.HandleWebhookAsync(token, webhook);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Processing the SumUp webhook for {ClientTransactionId} failed", webhook.Payload.ClientTransactionId);
        }
    }

    private static IResult ToResult(Result<PaymentStatusResponse> result)
        => result switch
        {
            { IsSuccess: true } => Results.Ok(result.Value),
            { Error.Code: "Payment.NotFound" } => Results.NotFound(result.Error),
            _ => Results.BadRequest(result.Error)
        };
}
