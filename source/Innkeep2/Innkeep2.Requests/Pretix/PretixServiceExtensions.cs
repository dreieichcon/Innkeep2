using System.Text.Json;
using Demolite.Http.Client;
using Demolite.Http.Extensions;
using Innkeep2.CredentialsDb;
using Innkeep2.CredentialsDb.Models;
using Innkeep2.Models.Serialization.Pretix;
using Innkeep2.Requests.Http;
using Innkeep2.Requests.Pretix.Clients;
using Microsoft.Extensions.DependencyInjection;

namespace Innkeep2.Requests.Pretix;

public static class PretixServiceCollectionExtensions
{
	public static void AddPretixSerializerOptions(this IServiceCollection services)
	{
		services.AddKeyedSingleton("pretix", (_, _) =>
		{
			var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
			options.Converters.Add(new PretixDecimalConverter());
			return options;
		});
	}

	public static void AddPretixClients(this IServiceCollection services)
	{
		services.AddPretixSerializerOptions();

		services.AddPretixClient<PretixOrganizerClient>();
		services.AddPretixClient<PretixEventClient>();
		services.AddPretixClient<PretixEventSettingsClient>();
		services.AddPretixClient<PretixSalesItemClient>();
		services.AddPretixClient<PretixQuotaClient>();
		services.AddPretixClient<PretixOrderClient>();
	}

	private static void AddPretixClient<TClient>(this IServiceCollection services)
		where TClient : ApiClient
		=> services
			.AddHttpClient<TClient>((sp, client) =>
				client.BaseAddress = GetCredential(sp) is { } credential
					? new Uri(credential.BaseUrl)
					: MissingCredentialHandler.PlaceholderAddress)
			.AddMissingCredentialHandler(sp => GetCredential(sp) is not null)
			.AddStaticAuth("Token", sp => GetCredential(sp)?.ApiKey ?? string.Empty);

	private static PretixCredential? GetCredential(IServiceProvider sp)
		=> sp.GetRequiredService<ActiveCredentialProvider<CredentialPair>>().Active?.Pretix;
}
