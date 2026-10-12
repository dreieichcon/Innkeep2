using Innkeep2.CredentialsDb.Models;
using Innkeep2.CredentialsDb.Repositories;

namespace Innkeep2.CredentialsDb.Tests;

[TestClass]
public class CredentialRepositoryTests
{
    private string _databasePath = null!;
    private InnkeepApiKeyRepository _repository = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"credentials-{Guid.NewGuid():N}.db");
        _repository = new InnkeepApiKeyRepository(_databasePath);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (File.Exists(_databasePath))
            File.Delete(_databasePath);
    }

    [TestMethod]
    public async Task Create_FirstItem_BecomesActive()
    {
        var created = await _repository.CreateAsync(NewKey("first"));

        Assert.IsTrue(created.IsSuccess);
        Assert.IsTrue(created.Value!.Active);
    }

    [TestMethod]
    public async Task Create_SecondInactiveItem_KeepsFirstActive()
    {
        var first = (await _repository.CreateAsync(NewKey("first"))).Value!;
        await _repository.CreateAsync(NewKey("second"));

        var active = await _repository.GetActiveAsync();

        Assert.AreEqual(first.Id, active.Value!.Id);
    }

    [TestMethod]
    public async Task Create_ActiveItem_DeactivatesOthers()
    {
        await _repository.CreateAsync(NewKey("first"));
        var second = (await _repository.CreateAsync(NewKey("second", active: true))).Value!;

        var all = (await _repository.GetAllAsync()).Value!;

        Assert.AreEqual(1, all.Count(x => x.Active));
        Assert.AreEqual(second.Id, all.Single(x => x.Active).Id);
    }

    [TestMethod]
    public async Task Update_ActivatingItem_DeactivatesOthers()
    {
        await _repository.CreateAsync(NewKey("first"));
        var second = (await _repository.CreateAsync(NewKey("second"))).Value!;

        second.Active = true;
        await _repository.UpdateAsync(second);

        var all = (await _repository.GetAllAsync()).Value!;

        Assert.AreEqual(1, all.Count(x => x.Active));
        Assert.AreEqual(second.Id, all.Single(x => x.Active).Id);
    }

    [TestMethod]
    public async Task Update_UnknownItem_Fails()
    {
        var result = await _repository.UpdateAsync(NewKey("unknown"));

        Assert.IsFalse(result.IsSuccess);
    }

    [TestMethod]
    public async Task SetActive_SwitchesExactlyOneItem()
    {
        await _repository.CreateAsync(NewKey("first"));
        await _repository.CreateAsync(NewKey("second"));
        var third = (await _repository.CreateAsync(NewKey("third"))).Value!;

        var result = await _repository.SetActiveAsync(third.Id);
        var all = (await _repository.GetAllAsync()).Value!;

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(1, all.Count(x => x.Active));
        Assert.AreEqual(third.Id, all.Single(x => x.Active).Id);
    }

    [TestMethod]
    public async Task SetActive_UnknownId_FailsAndKeepsActiveItem()
    {
        var first = (await _repository.CreateAsync(NewKey("first"))).Value!;

        var result = await _repository.SetActiveAsync(Guid.NewGuid());
        var active = await _repository.GetActiveAsync();

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(first.Id, active.Value!.Id);
    }

    [TestMethod]
    public async Task GetActive_EmptyDatabase_Fails()
    {
        var result = await _repository.GetActiveAsync();

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("Credentials.NoActive", result.Error!.Code);
    }

    [TestMethod]
    public async Task Delete_ActiveItem_IsAllowedAndLeavesNoActiveItem()
    {
        var first = (await _repository.CreateAsync(NewKey("first"))).Value!;
        await _repository.CreateAsync(NewKey("second"));

        var deleted = await _repository.DeleteAsync(first.Id);
        var active = await _repository.GetActiveAsync();

        Assert.IsTrue(deleted.IsSuccess);
        Assert.IsFalse(active.IsSuccess);
    }

    [TestMethod]
    public async Task Delete_UnknownId_Fails()
    {
        var result = await _repository.DeleteAsync(Guid.NewGuid());

        Assert.IsFalse(result.IsSuccess);
    }

    [TestMethod]
    public async Task GetById_UnknownId_Fails()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        Assert.IsFalse(result.IsSuccess);
    }

    [TestMethod]
    public async Task CredentialPair_RoundTripsWithNestedCredentials()
    {
        var pairRepository = new CredentialPairRepository(_databasePath);
        var pair = new CredentialPair
        {
            Name = "event",
            Pretix = new PretixCredential("pretix-key", "https://pretix.local"),
            Fiskaly = new FiskalyCredential("fiskaly-key", "fiskaly-secret")
        };

        await pairRepository.CreateAsync(pair);
        var loaded = (await pairRepository.GetActiveAsync()).Value!;

        Assert.AreEqual("event", loaded.Name);
        Assert.AreEqual(pair.Pretix, loaded.Pretix);
        Assert.AreEqual(pair.Fiskaly, loaded.Fiskaly);
    }

    private static InnkeepApiKey NewKey(string name, bool active = false) => new()
    {
        Name = name,
        ApiKey = $"key-{name}",
        Url = "https://example.local",
        Active = active
    };
}
