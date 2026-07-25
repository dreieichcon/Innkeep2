using System.Text.Json;
using Innkeep2.Credentials;
using Innkeep2.Credentials.Models;
using Innkeep2.Requests.Pretix.Clients;
using Innkeep2.Requests.Serialization.Pretix;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Innkeep2.Requests.Pretix;

public static class PretixServiceCollectionExtensions
{
	public static IServiceCollection AddPretixSerializerOptions(this IServiceCollection services)
	{
		services.AddSingleton(_ =>
		{
			var options = new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
			};
			options.Converters.Add(new PretixDecimalConverter());
			return options;
		});

		return services;
	}

	public static IServiceCollection AddPretixClients(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddPretixSerializerOptions();
		services.AddTransient<PretixAuthHandler>();

		services.AddHttpClient<PretixOrganizerClient>((sp, client) =>
			{
				var credentials = sp.GetRequiredService<ActiveCredentialsProvider<PretixCredential>>();
				client.BaseAddress = new Uri(credentials.GetActive().BaseUrl);
			})
			.AddHttpMessageHandler<PretixAuthHandler>();

		return services;
	}
}