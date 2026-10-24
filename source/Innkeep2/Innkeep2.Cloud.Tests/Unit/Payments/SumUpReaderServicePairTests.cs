using Demolite.Core.Models;
using Innkeep2.Cloud.Services.Payments;
using Innkeep2.CredentialsDb.Models;
using Innkeep2.Models.SumUp;
using NSubstitute;

namespace Innkeep2.Cloud.Tests.Unit.Payments;

[TestClass]
public class SumUpReaderServicePairTests
{
    private const string Code = "4WLFDSBF";

    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("1234567")]
    [DataRow("1234567890")]
    [DataRow("ABC-12345")]
    [DataRow("ABCD 1234")]
    [TestMethod]
    public async Task Pair_RejectsInvalidPairingCodesWithoutCallingSumUp(string? code)
    {
        var fixture = await SumUpReaderServiceFixture.CreateAsync();

        var result = await fixture.Service.PairAsync(code, "Frontdesk");

        Assert.AreEqual(SumUpPairingState.InvalidInput, result.State);
        await fixture.Client.DidNotReceiveWithAnyArgs().CreateReaderAsync(default!, default!, default);
    }

    [DataRow("4WLFDSBF")]
    [DataRow("4WLFDSBF1")]
    [DataRow("abcd1234")]
    [DataRow("  4WLFDSBF  ")]
    [TestMethod]
    public async Task Pair_AcceptsEightAndNineCharacterCodesAndTrimsThem(string code)
    {
        var fixture = await SumUpReaderServiceFixture.CreateAsync();
        fixture.CreateReturns(SumUpReaderServiceFixture.BuildReader("paired"));

        await fixture.Service.PairAsync(code, "Frontdesk");

        await fixture.Client.Received(1).CreateReaderAsync(
            SumUpReaderServiceFixture.MerchantCode,
            Arg.Is<SumUpCreateReaderRequest>(x => x.PairingCode == code.Trim()),
            Arg.Any<CancellationToken>());
    }

    [DataRow(null)]
    [DataRow("")]
    [DataRow("  ")]
    [TestMethod]
    public async Task Pair_RejectsBlankNamesWithoutCallingSumUp(string? name)
    {
        var fixture = await SumUpReaderServiceFixture.CreateAsync();

        var result = await fixture.Service.PairAsync(Code, name);

        Assert.AreEqual(SumUpPairingState.InvalidInput, result.State);
        await fixture.Client.DidNotReceiveWithAnyArgs().CreateReaderAsync(default!, default!, default);
    }

    [TestMethod]
    public async Task Pair_WithoutActiveAccount_FailsWithoutCallingSumUp()
    {
        var fixture = await SumUpReaderServiceFixture.CreateAsync(withAccount: false);

        var result = await fixture.Service.PairAsync(Code, "Frontdesk");

        Assert.AreEqual(SumUpPairingState.NoAccountCredential, result.State);
        await fixture.Client.DidNotReceiveWithAnyArgs().CreateReaderAsync(default!, default!, default);
    }

    [TestMethod]
    public async Task Pair_SendsTrimmedNameAndMerchantOfTheActiveAccount()
    {
        var fixture = await SumUpReaderServiceFixture.CreateAsync();
        fixture.CreateReturns(SumUpReaderServiceFixture.BuildReader("paired"));

        await fixture.Service.PairAsync(Code, "  Frontdesk  ");

        await fixture.Client.Received(1).CreateReaderAsync(
            SumUpReaderServiceFixture.MerchantCode,
            Arg.Is<SumUpCreateReaderRequest>(x => x.PairingCode == Code && x.Name == "Frontdesk"),
            Arg.Any<CancellationToken>());
    }

    [DataRow("Http.404", SumUpPairingState.InvalidCode)]
    [DataRow("Http.409", SumUpPairingState.CodeNotPending)]
    [DataRow("Http.401", SumUpPairingState.InvalidKey)]
    [DataRow("Http.403", SumUpPairingState.InvalidKey)]
    [DataRow("Http.400", SumUpPairingState.InvalidInput)]
    [DataRow("Http.500", SumUpPairingState.Unreachable)]
    [DataRow("Http.Timeout", SumUpPairingState.Unreachable)]
    [TestMethod]
    public async Task Pair_MapsCreateErrorsToStates(string errorCode, SumUpPairingState expected)
    {
        var fixture = await SumUpReaderServiceFixture.CreateAsync();
        fixture.CreateFails(errorCode);

        var result = await fixture.Service.PairAsync(Code, "Frontdesk");

        Assert.AreEqual(expected, result.State);
        Assert.IsFalse(result.IsPaired);
        await fixture.ReaderRepository.DidNotReceiveWithAnyArgs().CreateAsync(default!);
    }

    [TestMethod]
    public async Task Pair_AlreadyPairedOnCreate_SavesWithoutPolling()
    {
        var fixture = await SumUpReaderServiceFixture.CreateAsync();
        fixture.CreateReturns(SumUpReaderServiceFixture.BuildReader("paired"));

        var result = await fixture.Service.PairAsync(Code, "Frontdesk");

        Assert.AreEqual(SumUpPairingState.Paired, result.State);
        await fixture.Client.DidNotReceiveWithAnyArgs().GetReaderAsync(default!, default!, default);
    }

    [TestMethod]
    public async Task Pair_ExpiredOnCreate_FailsWithoutSaving()
    {
        var fixture = await SumUpReaderServiceFixture.CreateAsync();
        fixture.CreateReturns(SumUpReaderServiceFixture.BuildReader("expired"));

        var result = await fixture.Service.PairAsync(Code, "Frontdesk");

        Assert.AreEqual(SumUpPairingState.Expired, result.State);
        await fixture.ReaderRepository.DidNotReceiveWithAnyArgs().CreateAsync(default!);
    }

    [TestMethod]
    public async Task Pair_PollsUntilThePairingIsConfirmedAndSavesTheReader()
    {
        var fixture = await SumUpReaderServiceFixture.CreateAsync(maxPolls: 5);
        fixture.CreateReturns(SumUpReaderServiceFixture.BuildReader("processing"));
        fixture.GetReturns(
            SumUpReaderServiceFixture.Ok("processing"),
            SumUpReaderServiceFixture.Ok("processing"),
            SumUpReaderServiceFixture.Ok("paired"));

        var result = await fixture.Service.PairAsync(Code, "Frontdesk");

        Assert.AreEqual(SumUpPairingState.Paired, result.State);
        await fixture.Client.Received(3).GetReaderAsync(SumUpReaderServiceFixture.MerchantCode, "rdr_1", Arg.Any<CancellationToken>());
        await fixture.ReaderRepository.Received(1).CreateAsync(Arg.Is<SumUpReaderCredential>(x =>
            x.Name == "Frontdesk"
            && x.ReaderId == "rdr_1"
            && x.MerchantCode == SumUpReaderServiceFixture.MerchantCode
            && x.DeviceModel == "solo"
            && x.DeviceIdentifier == "serial-1"));
        Assert.AreEqual("Frontdesk", result.Reader!.Name);
    }

    [TestMethod]
    public async Task Pair_ReloadsTheActiveReaderCredentialAfterSaving()
    {
        var fixture = await SumUpReaderServiceFixture.CreateAsync();
        fixture.CreateReturns(SumUpReaderServiceFixture.BuildReader("paired"));

        await fixture.Service.PairAsync(Code, "Frontdesk");

        await fixture.ReaderRepository.Received(1).GetActiveAsync();
    }

    [TestMethod]
    public async Task Pair_PollingIsCaseInsensitiveForTheStatus()
    {
        var fixture = await SumUpReaderServiceFixture.CreateAsync();
        fixture.CreateReturns(SumUpReaderServiceFixture.BuildReader("processing"));
        fixture.GetReturns(SumUpReaderServiceFixture.Ok("PAIRED"));

        var result = await fixture.Service.PairAsync(Code, "Frontdesk");

        Assert.AreEqual(SumUpPairingState.Paired, result.State);
    }

    [TestMethod]
    public async Task Pair_ExpiredWhilePolling_FailsWithoutSaving()
    {
        var fixture = await SumUpReaderServiceFixture.CreateAsync();
        fixture.CreateReturns(SumUpReaderServiceFixture.BuildReader("processing"));
        fixture.GetReturns(SumUpReaderServiceFixture.Ok("processing"), SumUpReaderServiceFixture.Ok("expired"));

        var result = await fixture.Service.PairAsync(Code, "Frontdesk");

        Assert.AreEqual(SumUpPairingState.Expired, result.State);
        await fixture.ReaderRepository.DidNotReceiveWithAnyArgs().CreateAsync(default!);
    }

    [TestMethod]
    public async Task Pair_NeverConfirmed_TimesOutAfterTheConfiguredPollsWithoutSaving()
    {
        var fixture = await SumUpReaderServiceFixture.CreateAsync(maxPolls: 4);
        fixture.CreateReturns(SumUpReaderServiceFixture.BuildReader("processing"));
        fixture.GetReturns(SumUpReaderServiceFixture.Ok("processing"));

        var result = await fixture.Service.PairAsync(Code, "Frontdesk");

        Assert.AreEqual(SumUpPairingState.TimedOut, result.State);
        await fixture.Client.Received(4).GetReaderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await fixture.ReaderRepository.DidNotReceiveWithAnyArgs().CreateAsync(default!);
        await fixture.Client.DidNotReceiveWithAnyArgs().DeleteReaderAsync(default!, default!, default);
    }

    [TestMethod]
    public async Task Pair_ToleratesATemporaryPollingError()
    {
        var fixture = await SumUpReaderServiceFixture.CreateAsync();
        fixture.CreateReturns(SumUpReaderServiceFixture.BuildReader("processing"));
        fixture.GetReturns(SumUpReaderServiceFixture.Failed("Http.500"), SumUpReaderServiceFixture.Ok("paired"));

        var result = await fixture.Service.PairAsync(Code, "Frontdesk");

        Assert.AreEqual(SumUpPairingState.Paired, result.State);
    }

    [DataRow("Http.401")]
    [DataRow("Http.403")]
    [TestMethod]
    public async Task Pair_StopsPollingWhenTheKeyIsRejected(string errorCode)
    {
        var fixture = await SumUpReaderServiceFixture.CreateAsync();
        fixture.CreateReturns(SumUpReaderServiceFixture.BuildReader("processing"));
        fixture.GetReturns(SumUpReaderServiceFixture.Failed(errorCode));

        var result = await fixture.Service.PairAsync(Code, "Frontdesk");

        Assert.AreEqual(SumUpPairingState.InvalidKey, result.State);
        await fixture.Client.Received(1).GetReaderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Pair_StopsPollingWhenTheReaderDisappears()
    {
        var fixture = await SumUpReaderServiceFixture.CreateAsync();
        fixture.CreateReturns(SumUpReaderServiceFixture.BuildReader("processing"));
        fixture.GetReturns(SumUpReaderServiceFixture.Failed("Http.404"));

        var result = await fixture.Service.PairAsync(Code, "Frontdesk");

        Assert.AreEqual(SumUpPairingState.Unreachable, result.State);
        await fixture.Client.Received(1).GetReaderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Pair_CancellationStopsWaiting()
    {
        var fixture = await SumUpReaderServiceFixture.CreateAsync();
        fixture.CreateReturns(SumUpReaderServiceFixture.BuildReader("processing"));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() => fixture.Service.PairAsync(Code, "Frontdesk", cts.Token));
        await fixture.ReaderRepository.DidNotReceiveWithAnyArgs().CreateAsync(default!);
    }

    [TestMethod]
    public async Task Pair_FailsClearlyWhenTheReaderCannotBeSaved()
    {
        var fixture = await SumUpReaderServiceFixture.CreateAsync();
        fixture.CreateReturns(SumUpReaderServiceFixture.BuildReader("paired"));
        fixture.ReaderRepository.CreateAsync(Arg.Any<SumUpReaderCredential>())
            .Returns(Result<SumUpReaderCredential>.Failure(new Error("Db", "write failed")));

        var result = await fixture.Service.PairAsync(Code, "Frontdesk");

        Assert.AreEqual(SumUpPairingState.SaveFailed, result.State);
        Assert.IsNull(result.Reader);
    }

    [TestMethod]
    public async Task Pair_DoesNotSaveTheSameReaderTwice()
    {
        var fixture = await SumUpReaderServiceFixture.CreateAsync();
        fixture.CreateReturns(SumUpReaderServiceFixture.BuildReader("paired"));
        var saved = SumUpReaderServiceFixture.BuildSavedReader();
        fixture.ReaderRepository.GetAllAsync().Returns(Result<IReadOnlyList<SumUpReaderCredential>>.Success([saved]));

        var result = await fixture.Service.PairAsync(Code, "Frontdesk");

        Assert.AreEqual(SumUpPairingState.Paired, result.State);
        Assert.AreSame(saved, result.Reader);
        await fixture.ReaderRepository.DidNotReceiveWithAnyArgs().CreateAsync(default!);
    }

    [TestMethod]
    public void Options_ComputeThePollCountFromTimeoutAndInterval()
    {
        Assert.AreEqual(30, new SumUpPairingOptions().MaxPolls);
        Assert.AreEqual(5, new SumUpPairingOptions { PollInterval = TimeSpan.FromMilliseconds(1), Timeout = TimeSpan.FromMilliseconds(5) }.MaxPolls);
        Assert.AreEqual(3, new SumUpPairingOptions { PollInterval = TimeSpan.FromSeconds(2), Timeout = TimeSpan.FromSeconds(5) }.MaxPolls);
        Assert.AreEqual(1, new SumUpPairingOptions { PollInterval = TimeSpan.FromSeconds(10), Timeout = TimeSpan.FromSeconds(5) }.MaxPolls);
        Assert.AreEqual(1, new SumUpPairingOptions { PollInterval = TimeSpan.Zero }.MaxPolls);
    }
}
