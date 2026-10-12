using Microsoft.Extensions.DependencyInjection;

namespace Innkeep2.Requests.Http;

public static class HttpClientBuilderExtensions
{
    /// <summary>
    /// Short-circuits every request of the client while no credential is active.
    /// </summary>
    public static IHttpClientBuilder AddMissingCredentialHandler(
        this IHttpClientBuilder builder,
        Func<IServiceProvider, bool> hasCredential)
        => builder.AddHttpMessageHandler(provider => new MissingCredentialHandler(() => hasCredential(provider)));
}
