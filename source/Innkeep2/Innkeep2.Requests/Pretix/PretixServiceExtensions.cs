using System.Text.Json;
using Demolite.Http.Client;
using Demolite.Http.Extensions;
using Innkeep2.Credentials;
using Innkeep2.Credentials.Models;
using Innkeep2.Models.Serialization.Pretix;
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
			.AddApiClient<TClient>(sp => GetCredentials(sp).GetActive().BaseUrl)
			.AddStaticAuth("Token", sp => GetCredentials(sp).GetActive().ApiKey);

	private static ActiveCredentialsProvider<PretixCredential> GetCredentials(IServiceProvider sp)
		=> sp.GetRequiredService<ActiveCredentialsProvider<PretixCredential>>();
}
