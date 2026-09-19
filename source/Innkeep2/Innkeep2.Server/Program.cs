using Innkeep2.Server.Components;
using Innkeep2.Server.Extensions;
using Innkeep2.Server.Services;
using MudBlazor.Services;
using Serilog;

if (!Directory.Exists("./log"))
	Directory.CreateDirectory("./log");

Log.Logger = new LoggerConfiguration()
	.MinimumLevel.Debug()
	.WriteTo.Console()
	.WriteTo.Trace()
	.WriteTo.File("./log/log-.txt", rollingInterval: RollingInterval.Day)
	.CreateLogger();

var builder = WebApplication.CreateBuilder(args);

var credentialsPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "credentials", "credentials.server.json");
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

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode();


var startupService = app.Services.GetRequiredService<ServerStartupService>();

var startupResult = await startupService.RunAsync();

if (!startupResult)
	return;

await app.RunAsync();