using Innkeep2.Credentials.Models;
using Innkeep2.Credentials.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Innkeep2.Credentials;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddCredentialsConfiguration(this IServiceCollection services, IConfiguration config)
	{
		services.Configure<CredentialsOptions<PretixCredential>>(config.GetSection("pretix"));
		services.Configure<CredentialsOptions<FiskalyCredential>>(config.GetSection("fiskaly"));
		services.AddSingleton<ActiveCredentialsProvider<PretixCredential>>();
		services.AddSingleton<ActiveCredentialsProvider<FiskalyCredential>>();
		return services;
	}
}