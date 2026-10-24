using System.Text.Json;
using System.Text.Json.Serialization;
using Demolite.Http.Extensions;
using Innkeep2.CredentialsDb;
using Innkeep2.CredentialsDb.Models;
using Innkeep2.Requests.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Innkeep2.Requests.SumUp;

public static class SumUpServiceCollectionExtensions
{
	private const string BaseUrl = "https://api.sumup.com/";

	public static void AddSumUpClients(this IServiceCollection services)
	{
		services.AddKeyedSingleton("sumup", (_, _) => new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
		});

		services
			.AddApiClient<SumUpClient>(BaseUrl)
			.AddMissingCredentialHandler(sp => GetCredential(sp) is not null)
			.AddStaticAuth("Bearer", sp => GetCredential(sp)?.ApiKey ?? string.Empty);

		services.AddTransient<ISumUpClient>(sp => sp.GetRequiredService<SumUpClient>());
		services.AddTransient<ISumUpConnectionChecker>(sp => sp.GetRequiredService<SumUpConnectionChecker>());

		services.AddHttpClient<SumUpConnectionChecker>(client =>
		{
			client.BaseAddress = new Uri(BaseUrl);
			client.Timeout = TimeSpan.FromSeconds(10);
		});
	}

	private static SumUpCredential? GetCredential(IServiceProvider sp)
		=> sp.GetRequiredService<ActiveCredentialProvider<SumUpCredential>>().Active;
}
