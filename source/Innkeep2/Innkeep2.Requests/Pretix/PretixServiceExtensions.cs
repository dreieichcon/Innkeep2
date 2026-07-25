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
	extension(IServiceCollection services)
	{
		public void AddPretixSerializerOptions()
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
		}

		public void AddPretixClients()
		{
			services.AddPretixSerializerOptions();
			services.AddTransient<PretixAuthHandler>();

			services.AddHttpClient<PretixOrganizerClient>(ConfigureClient())
				.AddHttpMessageHandler<PretixAuthHandler>();
		
			services.AddHttpClient<PretixEventClient>(ConfigureClient())
				.AddHttpMessageHandler<PretixAuthHandler>();
		}
	}

	private static Action<IServiceProvider,HttpClient> ConfigureClient()
		=> (sp, client) =>
		{
			var credentials = sp.GetRequiredService<ActiveCredentialsProvider<PretixCredential>>();
			client.BaseAddress = new Uri(credentials.GetActive().BaseUrl);
		};
}