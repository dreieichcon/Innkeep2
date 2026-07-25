using Innkeep2.Credentials;
using Innkeep2.Credentials.Transformers;
using Innkeep2.Requests.Pretix;
using Innkeep2.Services.Cloud;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace Innkeep2.Cloud.Extensions;

public static class ServiceCollectionExtensions
{
	public static void RegisterCloudServices(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddKeycloakAuthentication(configuration);

		services.AddCredentialsConfiguration(configuration);
		services.AddPretixClients();
		services.AddMemoryCache();
		
		services.AddPretixCaches();
	}

	private static void AddPretixCaches(this IServiceCollection services)
	{
		services.AddSingleton<CachedPretixOrganizerProvider>();
	}

	private static void AddKeycloakAuthentication(this IServiceCollection services, IConfiguration configuration)
	{
		
		services.AddAuthentication(options =>
		{
			options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
			options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
		})
		.AddCookie()
		.AddOpenIdConnect(options =>
		{
			options.Authority = configuration["keycloak:authority"];
			options.ClientId = configuration["keycloak:clientid"];
			options.ClientSecret = configuration["keycloak:clientsecret"];
			options.ResponseType = "code";
			options.SaveTokens = true;
			options.GetClaimsFromUserInfoEndpoint = true;
			options.Scope.Add("openid");
			options.Scope.Add("profile");

			options.ClaimActions.MapUniqueJsonKey("resource_access", "resource_access");
		});
		
		services.AddSingleton<IClaimsTransformation, KeycloakRoleClaimsTransformation>();
		
		services.AddAuthorization();
		
		services.AddCascadingAuthenticationState();
	}
}