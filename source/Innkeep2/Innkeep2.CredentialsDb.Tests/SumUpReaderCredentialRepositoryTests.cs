using Innkeep2.CredentialsDb.Models;
using Innkeep2.CredentialsDb.Repositories;
using LiteDB;

namespace Innkeep2.CredentialsDb.Tests;

[TestClass]
public class SumUpReaderCredentialRepositoryTests
{
    private string _databasePath = null!;
    private SumUpReaderCredentialRepository _repository = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"credentials-{Guid.NewGuid():N}.db");
        _repository = new SumUpReaderCredentialRepository(_databasePath);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (File.Exists(_databasePath))
            File.Delete(_databasePath);
    }

    private static SumUpReaderCredential NewReader(string name, string readerId, bool active = false) => new()
    {
        Name = name,
        ReaderId = readerId,
        MerchantCode = "MH4H92C7",
        DeviceModel = "solo",
        DeviceIdentifier = "serial-1",
        Active = active
    };

    [TestMethod]
    public async Task GetActive_EmptyDatabase_Fails()
    {
        var result = await _repository.GetActiveAsync();

        Assert.IsFalse(result.IsSuccess);
    }

    [TestMethod]
    public async Task Create_FirstReader_BecomesActiveAndKeepsAllFields()
    {
        var created = await _repository.CreateAsync(NewReader("Front", "rdr_1"));

        var active = (await _repository.GetActiveAsync()).Value!;

        Assert.AreEqual(created.Value!.Id, active.Id);
        Assert.AreEqual("Front", active.Name);
        Assert.AreEqual("rdr_1", active.ReaderId);
        Assert.AreEqual("MH4H92C7", active.MerchantCode);
        Assert.AreEqual("solo", active.DeviceModel);
        Assert.AreEqual("serial-1", active.DeviceIdentifier);
        Assert.IsTrue(active.Active);
    }

    [TestMethod]
    public async Task Create_StoresThePairedTime()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var created = (await _repository.CreateAsync(NewReader("Front", "rdr_1"))).Value!;
        var loaded = (await _repository.GetByIdAsync(created.Id)).Value!;

        Assert.IsGreaterThanOrEqualTo(before, loaded.PairedAt.ToUniversalTime());
    }

    [TestMethod]
    public async Task Create_SecondReader_KeepsFirstActive()
    {
        var first = (await _repository.CreateAsync(NewReader("Front", "rdr_1"))).Value!;
        await _repository.CreateAsync(NewReader("Back", "rdr_2"));

        var active = (await _repository.GetActiveAsync()).Value!;

        Assert.AreEqual(first.Id, active.Id);
    }

    [TestMethod]
    public async Task SetActive_SwitchesTheActiveReader()
    {
        await _repository.CreateAsync(NewReader("Front", "rdr_1"));
        var second = (await _repository.CreateAsync(NewReader("Back", "rdr_2"))).Value!;

        await _repository.SetActiveAsync(second.Id);

        var all = (await _repository.GetAllAsync()).Value!;
        Assert.AreEqual(1, all.Count(x => x.Active));
        Assert.AreEqual("rdr_2", (await _repository.GetActiveAsync()).Value!.ReaderId);
    }

    [TestMethod]
    public async Task Update_RenamesTheReaderWithoutChangingTheReaderId()
    {
        var created = (await _repository.CreateAsync(NewReader("Front", "rdr_1"))).Value!;

        created.Name = "Kasse 1";
        await _repository.UpdateAsync(created);

        var loaded = (await _repository.GetByIdAsync(created.Id)).Value!;
        Assert.AreEqual("Kasse 1", loaded.Name);
        Assert.AreEqual("rdr_1", loaded.ReaderId);
    }

    [TestMethod]
    public async Task Delete_RemovesTheReader()
    {
        var created = (await _repository.CreateAsync(NewReader("Front", "rdr_1"))).Value!;

        await _repository.DeleteAsync(created.Id);

        Assert.IsEmpty((await _repository.GetAllAsync()).Value!);
    }

    [TestMethod]
    public async Task ReaderCredentials_AreStoredSeparatelyFromTheAccountCredentials()
    {
        var accounts = new SumUpCredentialRepository(_databasePath);
        await accounts.CreateAsync(new SumUpCredential { Name = "Account", ApiKey = "key", MerchantCode = "MH4H92C7", PublicBaseUrl = "https://cloud.example" });

        await _repository.CreateAsync(NewReader("Front", "rdr_1"));

        Assert.HasCount(1, (await accounts.GetAllAsync()).Value!);
        Assert.HasCount(1, (await _repository.GetAllAsync()).Value!);
    }

    [TestMethod]
    public async Task AccountCredentialsSavedWithAReaderId_StillLoad()
    {
        var id = Guid.NewGuid();
        using (var db = new LiteDatabase(_databasePath))
        {
            db.GetCollection("sumup").Insert(new BsonDocument
            {
                ["_id"] = id,
                ["Name"] = "Old",
                ["Active"] = true,
                ["ApiKey"] = "key",
                ["MerchantCode"] = "MH4H92C7",
                ["ReaderId"] = "rdr_legacy",
                ["PublicBaseUrl"] = "https://cloud.example",
                ["WebhookToken"] = "token"
            });
        }

        var accounts = new SumUpCredentialRepository(_databasePath);
        var active = await accounts.GetActiveAsync();

        Assert.IsTrue(active.IsSuccess);
        Assert.AreEqual("key", active.Value!.ApiKey);
        Assert.AreEqual("token", active.Value.WebhookToken);
    }
}
