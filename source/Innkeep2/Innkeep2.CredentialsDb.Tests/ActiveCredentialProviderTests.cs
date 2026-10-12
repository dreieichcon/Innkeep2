using Innkeep2.CredentialsDb.Models;
using Innkeep2.CredentialsDb.Repositories;

namespace Innkeep2.CredentialsDb.Tests;

[TestClass]
public class ActiveCredentialProviderTests
{
    private string _databasePath = null!;
    private InnkeepApiKeyRepository _repository = null!;
    private ActiveCredentialProvider<InnkeepApiKey> _provider = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"credentials-{Guid.NewGuid():N}.db");
        _repository = new InnkeepApiKeyRepository(_databasePath);
        _provider = new ActiveCredentialProvider<InnkeepApiKey>(_repository);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (File.Exists(_databasePath))
            File.Delete(_databasePath);
    }

    [TestMethod]
    public void GetActive_BeforeReload_Throws()
    {
        Assert.IsFalse(_provider.HasActive);
        Assert.Throws<InvalidOperationException>(() => _provider.GetActive());
    }

    [TestMethod]
    public async Task Reload_WithActiveCredential_ProvidesIt()
    {
        var created = (await _repository.CreateAsync(NewKey("first"))).Value!;

        await _provider.ReloadAsync();

        Assert.IsTrue(_provider.HasActive);
        Assert.AreEqual(created.Id, _provider.GetActive().Id);
    }

    [TestMethod]
    public async Task Reload_EmptyDatabase_HasNoActiveCredential()
    {
        await _provider.ReloadAsync();

        Assert.IsFalse(_provider.HasActive);
        Assert.Throws<InvalidOperationException>(() => _provider.GetActive());
    }

    [TestMethod]
    public async Task Reload_AfterSetActive_ProvidesNewCredentialAndRaisesChanged()
    {
        await _repository.CreateAsync(NewKey("first"));
        var second = (await _repository.CreateAsync(NewKey("second"))).Value!;
        await _provider.ReloadAsync();
        var changedCount = 0;
        _provider.Changed += (_, _) => changedCount++;

        await _repository.SetActiveAsync(second.Id);
        await _provider.ReloadAsync();

        Assert.AreEqual(second.Id, _provider.GetActive().Id);
        Assert.AreEqual(1, changedCount);
    }

    [TestMethod]
    public async Task GetActive_AfterDatabaseChangeWithoutReload_KeepsCachedCredential()
    {
        var first = (await _repository.CreateAsync(NewKey("first"))).Value!;
        var second = (await _repository.CreateAsync(NewKey("second"))).Value!;
        await _provider.ReloadAsync();

        await _repository.SetActiveAsync(second.Id);

        Assert.AreEqual(first.Id, _provider.GetActive().Id);
    }

    private static InnkeepApiKey NewKey(string name) => new()
    {
        Name = name,
        ApiKey = $"key-{name}",
        Url = "https://example.local"
    };
}
