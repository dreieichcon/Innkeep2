using Innkeep2.Cloud.Services;

namespace Innkeep2.Cloud.Api;

public sealed class ApiKeyFilter(ApiKeyValidationService validationService) : IEndpointFilter
{
    private const string HeaderName = "X-Api-Key";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var key) ||
            !await validationService.ValidateApiKey(key!))
            return Results.Unauthorized();

        return await next(context);
    }
}