using Innkeep2.Cloud.Components;
using Innkeep2.Cloud.Extensions;
using Innkeep2.Cloud.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

var credentialsPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "credentials", "credentials.json");
builder.Configuration.AddJsonFile(credentialsPath, optional: false, reloadOnChange: true);

builder.Services.AddDataProtection()
	.PersistKeysToFileSystem(new DirectoryInfo("./dataprotection-keys"))
	.SetApplicationName("Innkeep2.Cloud");

// Add services to the container.
builder.Services.AddRazorComponents()
	.AddInteractiveServerComponents();

builder.Services.AddMudServices();
builder.Services.AddScoped<UiResultHandler>();

builder.Services.RegisterCloudServices(builder.Configuration);
builder.Services.RegisterDatabaseServices();

builder.Services.AddSingleton<StatusBarService>();

var app = builder.Build();

var options = new ForwardedHeadersOptions
{
	ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
options.KnownIPNetworks.Clear();
options.KnownProxies.Clear();

app.UseForwardedHeaders(options);

await app.Services.MigrateDatabaseAsync();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error", createScopeForErrors: true);
	app.UseHsts();
}

# if Debug
app.UseHttpsRedirection();
#endif

app.UseAuthentication();
app.UseAuthorization();

app.UseStatusCodePages(async context =>
{
	if (context.HttpContext.Response.StatusCode == StatusCodes.Status403Forbidden)
		context.HttpContext.Response.Redirect("/access-denied");
	else if (context.HttpContext.Response.StatusCode == StatusCodes.Status404NotFound)
		context.HttpContext.Response.Redirect("/not-found");
});

app.UseAntiforgery();

app.MapStaticAssets();

app.MapGet("/Account/Login", (HttpContext _) =>
		Results.Challenge(
			new AuthenticationProperties { RedirectUri = "/" },
			[OpenIdConnectDefaults.AuthenticationScheme]))
	.AllowAnonymous();

app.MapPost("/Account/Logout", async context =>
	{
		await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
		await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
	})
	.RequireAuthorization();

# if DEBUG
app.MapGet("/debug/claims", (HttpContext ctx) =>
		string.Join("\n", ctx.User.Claims.Select(c => $"{c.Type}: {c.Value}")))
	.RequireAuthorization(new AuthorizationPolicyBuilder()
		.RequireAuthenticatedUser()
		.Build());

# endif

app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode();

await app.RunAsync();