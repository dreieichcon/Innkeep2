using Innkeep2.Credentials.ApiKeys;

namespace Innkeep2.Server.Security;

public sealed class ApiKeyFilter(ApiKeyRepository repository) : IEndpointFilter
{
    private const string HeaderName = "X-Api-Key";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var key))
            return Results.Unauthorized();

        var hash = ApiKeyHasher.Hash(key!);

        if (!repository.ValidateApiKey(hash))
            return Results.Unauthorized();

        return await next(context);
    }
}