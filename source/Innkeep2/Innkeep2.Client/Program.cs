using Innkeep2.Client.Components;
using Innkeep2.Client.Extensions;
using Innkeep2.Client.Services;
using Innkeep2.Credentials;
using Innkeep2.Services.Shared;
using MudBlazor.Services;
using Serilog;

AppSetup.SetupLogging();

var builder = WebApplication.CreateBuilder(args);

if (AppSetup.SetupCredentials("client", CredentialCreator.ClientJsonTemplate, out var credentialsPath))
{
    Log.Warning("No credentials file found. A template was created at {Path}. Fill it in and restart.", credentialsPath);
    return;
}

builder.Configuration.AddJsonFile(credentialsPath, optional: false, reloadOnChange: true);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

builder.Services.RegisterClientServices(builder.Configuration);

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

var startupService = app.Services.GetRequiredService<ClientStartupService>();

var startupResult = await startupService.RunAsync();

if (!startupResult)
    return;

await app.RunAsync();