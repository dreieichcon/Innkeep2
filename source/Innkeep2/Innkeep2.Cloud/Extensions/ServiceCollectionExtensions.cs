using Innkeep2.Cloud.AppDb;
using Innkeep2.Cloud.AppDb.Repositories;
using Innkeep2.Cloud.Orders;
using Innkeep2.Cloud.Services;
using Innkeep2.Cloud.TransactionDb;
using Innkeep2.Cloud.TransactionDb.Repositories;
using Innkeep2.Credentials;
using Innkeep2.Credentials.Transformers;
using Innkeep2.Requests.Fiskaly;
using Innkeep2.Requests.Pretix;
using Innkeep2.Services.Cloud.Cache;
using Innkeep2.Services.Cloud.Fiskaly;
using Innkeep2.Services.Cloud.Pretix;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;

namespace Innkeep2.Cloud.Extensions;

public static class ServiceCollectionExtensions
{
	public static void RegisterCloudServices(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddKeycloakAuthentication(configuration);

		services.AddCredentialsConfiguration(configuration);
		services.AddPretixClients();
		services.AddFiskalyClients();
		services.AddMemoryCache();
		
		services.AddPretixCaches();
		services.AddFiskalyServices();

		services.AddSingleton<ApiKeyValidationService>();
		services.AddOrderServices();
	}

	public static void RegisterDatabaseServices(this IServiceCollection services)
	{
		if (!Directory.Exists("./db"))
			Directory.CreateDirectory("./db");
		
		services.AddDbContextFactory<InnkeepCloudDbContext>(options =>
			options.UseSqlite("Data Source=./db/innkeepSettings.db"));

		services.AddSingleton<InnkeepCloudSettingsRepository>();
		services.AddSingleton<InnkeepCloudApiKeyRepository>();

		services.AddSingleton<IDbContextFactory<InnkeepOrderDbContext>, InnkeepOrderDbContextFactory>();
		services.AddSingleton<OrderRepository>();
	}
	
	public static async Task MigrateDatabaseAsync(this IServiceProvider services)
	{
		var factory = services.GetRequiredService<IDbContextFactory<InnkeepCloudDbContext>>();
		await using var context = await factory.CreateDbContextAsync();
		await context.Database.MigrateAsync();
	}

	private static void AddPretixCaches(this IServiceCollection services)
	{
		services.AddSingleton<CachedOrganizerProvider>();
		services.AddSingleton<CachedEventProvider>();
		services.AddSingleton<CachedSalesItemProvider>();
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

	private static void AddFiskalyServices(this IServiceCollection services)
	{
		services.AddSingleton<TssService>();
		services.AddSingleton<ClientService>();
		services.AddSingleton<FiskalyTransactionService>();
	}
	
	private static void AddOrderServices(this IServiceCollection services)
	{
		services.AddSingleton<PretixOrderService>();
		services.AddSingleton<OrderService>();
		services.AddSingleton<OrderDashboardService>();
	}
}