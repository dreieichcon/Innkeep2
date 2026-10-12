using Innkeep2.CredentialsDb;
using Innkeep2.CredentialsDb.Models;
using Innkeep2.Server.Api;
using Innkeep2.Server.Components;
using Innkeep2.Server.Extensions;
using Innkeep2.Server.Security;
using Innkeep2.Server.Services;
using Innkeep2.Services.Shared;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using MudBlazor.Services;
using Serilog;


var builder = WebApplication.CreateBuilder(args);

AppSetup.SetupLogging("Innkeep2.Server");
builder.Logging.UseSerilogOnly();

builder.Services.AddDataProtection()
	.PersistKeysToFileSystem(new DirectoryInfo("./dataprotection-keys"))
	.SetApplicationName("Innkeep2.Server");

// Add services to the container.
builder.Services.AddRazorComponents()
	.AddInteractiveServerComponents();

builder.Services.AddMudServices();

builder.Services.RegisterServerServices();

var app = builder.Build();

await app.Services.LoadCredentialsAsync<InnkeepApiKey>(AppSetup.ConfigureDiscord);

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