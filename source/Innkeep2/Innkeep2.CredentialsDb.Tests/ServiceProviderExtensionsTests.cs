using Innkeep2.CredentialsDb.Models;
using Innkeep2.CredentialsDb.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Innkeep2.CredentialsDb.Tests;

[TestClass]
public class ServiceProviderExtensionsTests
{
    private string _databasePath = null!;
    private DiscordCredentialRepository _discordRepository = null!;
    private InnkeepApiKeyRepository _apiKeyRepository = null!;
    private ServiceProvider _services = null!;
    private List<string?> _webhooks = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"credentials-{Guid.NewGuid():N}.db");
        _discordRepository = new DiscordCredentialRepository(_databasePath);
        _apiKeyRepository = new InnkeepApiKeyRepository(_databasePath);
        _webhooks = [];

        var collection = new ServiceCollection();
        collection.AddSingleton<Interfaces.ICredentialRepository<DiscordCredential>>(_discordRepository);
        collection.AddSingleton<Interfaces.ICredentialRepository<InnkeepApiKey>>(_apiKeyRepository);
        collection.AddSingleton<ActiveCredentialProvider<DiscordCredential>>();
        collection.AddSingleton<ActiveCredentialProvider<InnkeepApiKey>>();
        _services = collection.BuildServiceProvider();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _services.Dispose();

        if (File.Exists(_databasePath))
            File.Delete(_databasePath);
    }

    [TestMethod]
    public async Task Load_WithoutDiscordCredential_ReportsNoWebhook()
    {
        await _services.LoadCredentialsAsync<InnkeepApiKey>(_webhooks.Add);

        CollectionAssert.AreEqual(new List<string?> { null }, _webhooks);
    }

    [TestMethod]
    public async Task Load_WithDiscordCredential_ReportsActiveWebhook()
    {
        await _discordRepository.CreateAsync(NewDiscord("https://discord.local/hook"));

        await _services.LoadCredentialsAsync<InnkeepApiKey>(_webhooks.Add);

        CollectionAssert.AreEqual(new List<string?> { "https://discord.local/hook" }, _webhooks);
    }

    [TestMethod]
    public async Task Load_ProvidesActiveCredentialOfMainType()
    {
        var created = (await _apiKeyRepository.CreateAsync(NewApiKey())).Value!;

        await _services.LoadCredentialsAsync<InnkeepApiKey>(_webhooks.Add);

        var provider = _services.GetRequiredService<ActiveCredentialProvider<InnkeepApiKey>>();
        Assert.AreEqual(created.Id, provider.GetActive().Id);
    }

    [TestMethod]
    public async Task DiscordReload_AfterLoad_ReportsNewWebhook()
    {
        await _services.LoadCredentialsAsync<InnkeepApiKey>(_webhooks.Add);

        await _discordRepository.CreateAsync(NewDiscord("https://discord.local/new"));
        await _services.GetRequiredService<ActiveCredentialProvider<DiscordCredential>>().ReloadAsync();

        Assert.AreEqual("https://discord.local/new", _webhooks[^1]);
    }

    private static DiscordCredential NewDiscord(string webhookUrl) => new() { Name = "logs", WebhookUrl = webhookUrl };

    private static InnkeepApiKey NewApiKey() => new() { Name = "key", ApiKey = "secret", Url = "https://example.local" };
}
