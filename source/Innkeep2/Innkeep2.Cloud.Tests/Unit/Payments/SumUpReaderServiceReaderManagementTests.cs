using Demolite.Core.Models;
using Innkeep2.Cloud.Services.Payments;
using Innkeep2.CredentialsDb.Models;
using Innkeep2.Models.SumUp;
using Innkeep2.Requests.SumUp;
using NSubstitute;

namespace Innkeep2.Cloud.Tests.Unit.Payments;

[TestClass]
public class SumUpReaderServiceReaderManagementTests
{
    private static Result<SumUpReaderList> ListOf(params SumUpReader[] readers)
        => Result<SumUpReaderList>.Success(new SumUpReaderList { Items = readers });

    #region Adoptable readers

    [TestMethod]
    public async Task GetAdoptableReaders_ReturnsPairedReadersThatAreNotSavedYet()
    {
        var fixture = await SumUpReaderServiceFixture.CreateAsync();
        fixture.Client.ListReadersAsync(SumUpReaderServiceFixture.MerchantCode, Arg.Any<CancellationToken>()).Returns(ListOf(
            SumUpReaderServiceFixture.BuildReader("paired", "rdr_saved"),
            SumUpReaderServiceFixture.BuildReader("paired", "rdr_new"),
            SumUpReaderServiceFixture.BuildReader("processing", "rdr_processing"),
            SumUpReaderServiceFixture.BuildReader("expired", "rdr_expired"),
            SumUpReaderServiceFixture.BuildReader("unknown", "rdr_unknown")));
        fixture.ReaderRepository.GetAllAsync().Returns(
            Result<IReadOnlyList<SumUpReaderCredential>>.Success([SumUpReaderServiceFixture.BuildSavedReader("rdr_saved")]));

        var result = await fixture.Service.GetAdoptableReadersAsync();

        Assert.IsTrue(result.IsSuccess);
        Assert.HasCount(1, result.Value!);
        Assert.AreEqual("rdr_new", result.Value[0].Id);
    }

    [TestMethod]
    public async Task GetAdoptableReaders_ReturnsAnEmptyListWhenEverythingIsSaved()
    {
        var fixture = await SumUpReaderServiceFixture.CreateAsync();
        fixture.Client.ListReadersAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ListOf(SumUpReaderServiceFixture.BuildReader("paired", "rdr_saved")));
        fixture.ReaderRepository.GetAllAsync().Returns(
            Result<IReadOnlyList<SumUpReaderCredential>>.Success([SumUpReaderServiceFixture.BuildSavedReader("rdr_saved")]));

        var result = await fixture.Service.GetAdoptableReadersAsync();

        Assert.IsTrue(result.IsSuccess);
        Assert.IsEmpty(result.Value!);
    }

    [TestMethod]
    public async Task GetAdoptableReaders_WithoutActiveAccount_FailsWithoutCallingSumUp()
    {
        var fixture = await SumUpReaderServiceFixture.CreateAsync(withAccount: false);

        var result = await fixture.Service.GetAdoptableReadersAsync();

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("SumUp.NoAccount", result.Error!.Code);
        await fixture.Client.DidNotReceiveWithAnyArgs().ListReadersAsync(default!, default);
    }

    [DataRow("Http.401", "Http.401")]
    [DataRow("Http.403", "Http.403")]
    [DataRow("Http.404", "Http.404")]
    [DataRow("Http.500", "Http.500")]
    [TestMethod]
    public async Task GetAdoptableReaders_ReturnsFailureWhenSumUpFails(string errorCode, string expectedCode)
    {
        var fixture = await SumUpReaderServiceFixture.CreateAsync();
        fixture.Client.ListReadersAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<SumUpReaderList>.Failure(new Error(errorCode, "raw")));

        var result = await fixture.Service.GetAdoptableReadersAsync();

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(expectedCode, result.Error!.Code);
        Assert.AreNotEqual("raw", result.Error.Message);
    }

    [TestMethod]
    public async Task GetAdoptableReaders_ReturnsFailureWhenSavedReadersCannotBeLoaded()
    {
        var fixture = await SumUpReaderServiceFixture.CreateAsync();
        fixture.Client.ListReadersAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(ListOf());
        fixture.ReaderRepository.GetAllAsync().Returns(Result<IReadOnlyList<SumUpReaderCredential>>.Failure(new Error("Db", "read failed")));

        var result = await fixture.Service.GetAdoptableReadersAsync();

        Assert.IsFalse(result.IsSuccess);
    }

    #endregion

    #region Adopt

    [TestMethod]
    public async Task AdoptReader_SavesThePairedReaderWithTheGivenName()
    {
        var fixture = await SumUpReaderServiceFixture.CreateAsync();

        var result = await fixture.Service.AdoptReaderAsync(SumUpReaderServiceFixture.BuildReader("paired", "rdr_new"), "  Kasse 2  ");

        Assert.IsTrue(result.IsSuccess);
        await fixture.ReaderRepository.Received(1).CreateAsync(Arg.Is<SumUpReaderCredential>(x =>
            x.Name == "Kasse 2"
            && x.ReaderId == "rdr_new"
            && x.MerchantCode == SumUpReaderServiceFixture.MerchantCode
            && x.DeviceModel == "solo"));
        await fixture.ReaderRepository.Received(1).GetActiveAsync();
    }

    [DataRow(null)]
    [DataRow("")]
    [DataRow("  ")]
    [TestMethod]
    public async Task AdoptReader_UsesTheSumUpNameWhenNoNameIsGiven(string? name)
    {
        var fixture = await SumUpReaderServiceFixture.CreateAsync();

        var result = await fixture.Service.AdoptReaderAsync(SumUpReaderServiceFixture.BuildReader("paired", "rdr_new", "From SumUp"), name);

        Assert.AreEqual("From SumUp", result.Value!.Name);
    }

    [TestMethod]
    public async Task AdoptReader_UsesTheReaderIdWhenThereIsNoNameAtAll()
    {
        var fixture = await SumUpReaderServiceFixture.CreateAsync();

        var result = await fixture.Service.AdoptReaderAsync(SumUpReaderServiceFixture.BuildReader("paired", "rdr_new", null), null);

        Assert.AreEqual("rdr_new", result.Value!.Name);
    }

    [DataRow("processing")]
    [DataRow("expired")]
    [DataRow("unknown")]
    [TestMethod]
    public async Task AdoptReader_RejectsReadersThatAreNotPaired(string status)
    {
        var fixture = await SumUpReaderServiceFixture.CreateAsync();

        var result = await fixture.Service.AdoptReaderAsync(SumUpReaderServiceFixture.BuildReader(status), "Name");

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("SumUp.NotPaired", result.Error!.Code);
        await fixture.ReaderRepository.DidNotReceiveWithAnyArgs().CreateAsync(default!);
    }

    [TestMethod]
    public async Task AdoptReader_WithoutActiveAccount_Fails()
    {
        var fixture = await SumUpReaderServiceFixture.CreateAsync(withAccount: false);

        var result = await fixture.Service.AdoptReaderAsync(SumUpReaderServiceFixture.BuildReader("paired"), "Name");

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("SumUp.NoAccount", result.Error!.Code);
    }

    [TestMethod]
    public async Task AdoptReader_ReturnsFailureWhenSavingFails()
    {
        var fixture = await SumUpReaderServiceFixture.CreateAsync();
        fixture.ReaderRepository.CreateAsync(Arg.Any<SumUpReaderCredential>())
            .Returns(Result<SumUpReaderCredential>.Failure(new Error("Db", "write failed")));

        var result = await fixture.Service.AdoptReaderAsync(SumUpReaderServiceFixture.BuildReader("paired"), "Name");

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("SumUp.SaveFailed", result.Error!.Code);
    }

    #endregion

    #region Unpair

    private static SumUpReaderCredential SavedReader() => SumUpReaderServiceFixture.BuildSavedReader("rdr_1");

    [TestMethod]
    public async Task Unpair_DeletesAtSumUpAndThenLocally()
    {
        var fixture = await SumUpReaderServiceFixture.CreateAsync();
        var reader = SavedReader();
        fixture.Client.DeleteReaderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await fixture.Service.UnpairAsync(reader);

        Assert.IsTrue(result.IsSuccess);
        Received.InOrder(() =>
        {
            fixture.Client.DeleteReaderAsync(SumUpReaderServiceFixture.MerchantCode, "rdr_1", Arg.Any<CancellationToken>());
            fixture.ReaderRepository.DeleteAsync(reader.Id);
        });
        await fixture.ReaderRepository.Received(1).GetActiveAsync();
    }

    [TestMethod]
    public async Task Unpair_UsesTheMerchantOfTheReader()
    {
        var fixture = await SumUpReaderServiceFixture.CreateAsync();
        var reader = new SumUpReaderCredential { Name = "Other", ReaderId = "rdr_x", MerchantCode = "OTHER123" };
        fixture.Client.DeleteReaderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Result.Success());

        await fixture.Service.UnpairAsync(reader);

        await fixture.Client.Received(1).DeleteReaderAsync("OTHER123", "rdr_x", Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Unpair_StillRemovesLocallyWhenSumUpDoesNotKnowTheReader()
    {
        var fixture = await SumUpReaderServiceFixture.CreateAsync();
        var reader = SavedReader();
        fixture.Client.DeleteReaderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Http.404", "not found")));

        var result = await fixture.Service.UnpairAsync(reader);

        Assert.IsTrue(result.IsSuccess);
        await fixture.ReaderRepository.Received(1).DeleteAsync(reader.Id);
    }

    [DataRow("Http.401")]
    [DataRow("Http.403")]
    [DataRow("Http.500")]
    [DataRow("Http.Timeout")]
    [TestMethod]
    public async Task Unpair_KeepsTheLocalReaderWhenSumUpFails(string errorCode)
    {
        var fixture = await SumUpReaderServiceFixture.CreateAsync();
        fixture.Client.DeleteReaderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error(errorCode, "raw")));

        var result = await fixture.Service.UnpairAsync(SavedReader());

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(errorCode, result.Error!.Code);
        await fixture.ReaderRepository.DidNotReceiveWithAnyArgs().DeleteAsync(default);
    }

    [TestMethod]
    public async Task Unpair_WithoutActiveAccount_ChangesNothing()
    {
        var fixture = await SumUpReaderServiceFixture.CreateAsync(withAccount: false);

        var result = await fixture.Service.UnpairAsync(SavedReader());

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("SumUp.NoAccount", result.Error!.Code);
        await fixture.Client.DidNotReceiveWithAnyArgs().DeleteReaderAsync(default!, default!, default);
        await fixture.ReaderRepository.DidNotReceiveWithAnyArgs().DeleteAsync(default);
    }

    [TestMethod]
    public async Task Unpair_ReturnsFailureWhenTheLocalDeleteFails()
    {
        var fixture = await SumUpReaderServiceFixture.CreateAsync();
        fixture.Client.DeleteReaderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Result.Success());
        fixture.ReaderRepository.DeleteAsync(Arg.Any<Guid>()).Returns(Result.Failure(new Error("Db", "delete failed")));

        var result = await fixture.Service.UnpairAsync(SavedReader());

        Assert.IsFalse(result.IsSuccess);
        await fixture.ReaderRepository.DidNotReceive().GetActiveAsync();
    }

    #endregion

    #region Saved readers, activation and check

    [TestMethod]
    public async Task GetSavedReaders_ReturnsTheSavedReaders()
    {
        var fixture = await SumUpReaderServiceFixture.CreateAsync();
        var saved = SumUpReaderServiceFixture.BuildSavedReader();
        fixture.ReaderRepository.GetAllAsync().Returns(Result<IReadOnlyList<SumUpReaderCredential>>.Success([saved]));

        var result = await fixture.Service.GetSavedReadersAsync();

        Assert.AreSame(saved, result.Value!.Single());
    }

    [TestMethod]
    public async Task SetActive_ActivatesTheReaderAndReloadsTheActiveCredential()
    {
        var fixture = await SumUpReaderServiceFixture.CreateAsync();
        var reader = SumUpReaderServiceFixture.BuildSavedReader();
        fixture.ReaderRepository.SetActiveAsync(reader.Id).Returns(Result.Success());

        var result = await fixture.Service.SetActiveAsync(reader);

        Assert.IsTrue(result.IsSuccess);
        await fixture.ReaderRepository.Received(1).SetActiveAsync(reader.Id);
        await fixture.ReaderRepository.Received(1).GetActiveAsync();
    }

    [TestMethod]
    public async Task SetActive_ReturnsFailureAndDoesNotReloadWhenActivationFails()
    {
        var fixture = await SumUpReaderServiceFixture.CreateAsync();
        fixture.ReaderRepository.SetActiveAsync(Arg.Any<Guid>()).Returns(Result.Failure(new Error("Db", "failed")));

        var result = await fixture.Service.SetActiveAsync(SumUpReaderServiceFixture.BuildSavedReader());

        Assert.IsFalse(result.IsSuccess);
        await fixture.ReaderRepository.DidNotReceive().GetActiveAsync();
    }

    [TestMethod]
    public async Task CheckReader_ChecksTheReaderWithTheKeyOfTheActiveAccount()
    {
        var fixture = await SumUpReaderServiceFixture.CreateAsync();
        var expected = new SumUpConnectionCheckResult(SumUpConnectionState.Connected, "ok");
        fixture.Checker.CheckReaderAsync("key", SumUpReaderServiceFixture.MerchantCode, "rdr_1", Arg.Any<CancellationToken>()).Returns(expected);

        var result = await fixture.Service.CheckReaderAsync(SumUpReaderServiceFixture.BuildSavedReader("rdr_1"));

        Assert.AreSame(expected, result);
    }

    [TestMethod]
    public async Task CheckReader_WithoutActiveAccount_DoesNotCallSumUp()
    {
        var fixture = await SumUpReaderServiceFixture.CreateAsync(withAccount: false);

        var result = await fixture.Service.CheckReaderAsync(SumUpReaderServiceFixture.BuildSavedReader());

        Assert.AreEqual(SumUpConnectionState.Incomplete, result.State);
        await fixture.Checker.DidNotReceiveWithAnyArgs().CheckReaderAsync(default, default, default, default);
    }

    [TestMethod]
    public async Task CheckReader_ForAReaderOfAnotherMerchant_DoesNotCallSumUp()
    {
        var fixture = await SumUpReaderServiceFixture.CreateAsync();
        var reader = new SumUpReaderCredential { Name = "Other", ReaderId = "rdr_x", MerchantCode = "OTHER123" };

        var result = await fixture.Service.CheckReaderAsync(reader);

        Assert.AreEqual(SumUpConnectionState.ReaderNotFound, result.State);
        await fixture.Checker.DidNotReceiveWithAnyArgs().CheckReaderAsync(default, default, default, default);
    }

    #endregion
}
