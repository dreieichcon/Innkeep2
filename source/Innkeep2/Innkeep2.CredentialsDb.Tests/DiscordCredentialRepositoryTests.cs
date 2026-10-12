using Innkeep2.CredentialsDb.Models;
using Innkeep2.CredentialsDb.Repositories;

namespace Innkeep2.CredentialsDb.Tests;

[TestClass]
public class DiscordCredentialRepositoryTests
{
    private string _databasePath = null!;
    private DiscordCredentialRepository _repository = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"credentials-{Guid.NewGuid():N}.db");
        _repository = new DiscordCredentialRepository(_databasePath);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (File.Exists(_databasePath))
            File.Delete(_databasePath);
    }

    [TestMethod]
    public async Task GetActive_EmptyDatabase_Fails()
    {
        var result = await _repository.GetActiveAsync();

        Assert.IsFalse(result.IsSuccess);
    }

    [TestMethod]
    public async Task Create_FirstCredential_IsActiveWithWebhook()
    {
        await _repository.CreateAsync(new DiscordCredential { Name = "logs", WebhookUrl = "https://discord.local/hook" });

        var active = (await _repository.GetActiveAsync()).Value!;

        Assert.AreEqual("https://discord.local/hook", active.WebhookUrl);
    }
}
