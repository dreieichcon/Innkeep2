using Innkeep2.Credentials;
using Innkeep2.Server.Api;
using Innkeep2.Server.Components;
using Innkeep2.Server.Extensions;
using Innkeep2.Server.Security;
using Innkeep2.Server.Services;
using Innkeep2.Services.Shared;
using Microsoft.AspNetCore.HttpOverrides;
using MudBlazor.Services;
using Serilog;

AppSetup.SetupLogging();

var builder = WebApplication.CreateBuilder(args);

if (AppSetup.SetupCredentials("server", CredentialCreator.ServerJsonTemplate, out var credentialsPath))
{
	Log.Warning("No credentials file found. A template was created at {Path}. Fill it in and restart.", credentialsPath);
	return;
}

builder.Configuration.AddJsonFile(credentialsPath, optional: false, reloadOnChange: true);

// Add services to the container.
builder.Services.AddRazorComponents()
	.AddInteractiveServerComponents();

builder.Services.AddMudServices();

builder.Services.RegisterServerServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error", createScopeForErrors: true);

	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseWhen(
	context => !context.Request.Path.StartsWithSegments("/api"),
	branch => branch.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true)
);

# if DEBUG
app.UseHttpsRedirection();
#endif 

# if RELEASE
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
	ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
# endif

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode();

app.MapGroup("/api/v1")
	.AddEndpointFilter<ApiKeyFilter>()
	.MapApiEndpoints();

var startupService = app.Services.GetRequiredService<ServerStartupService>();

var startupResult = await startupService.RunAsync();

if (!startupResult)
	return;

await app.RunAsync();