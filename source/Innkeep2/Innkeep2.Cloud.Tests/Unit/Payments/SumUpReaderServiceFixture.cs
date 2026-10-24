using Demolite.Core.Models;
using Innkeep2.Cloud.Services.Payments;
using Innkeep2.CredentialsDb;
using Innkeep2.CredentialsDb.Interfaces;
using Innkeep2.CredentialsDb.Models;
using Innkeep2.Models.SumUp;
using Innkeep2.Requests.SumUp;
using NSubstitute;

namespace Innkeep2.Cloud.Tests.Unit.Payments;

internal sealed class SumUpReaderServiceFixture
{
    public const string MerchantCode = "MH4H92C7";

    private SumUpReaderServiceFixture(SumUpPairingOptions options)
    {
        Account = new ActiveCredentialProvider<SumUpCredential>(AccountRepository);
        Readers = new ActiveCredentialProvider<SumUpReaderCredential>(ReaderRepository);
        Service = new SumUpReaderService(Client, Account, ReaderRepository, Readers, options);

        ReaderRepository.GetAllAsync().Returns(Result<IReadOnlyList<SumUpReaderCredential>>.Success([]));
        ReaderRepository.GetActiveAsync().Returns(Result<SumUpReaderCredential>.Failure(new Error("NotFound", "none")));
        ReaderRepository.CreateAsync(Arg.Any<SumUpReaderCredential>())
            .Returns(call => Result<SumUpReaderCredential>.Success(call.Arg<SumUpReaderCredential>()));
        ReaderRepository.DeleteAsync(Arg.Any<Guid>()).Returns(Result.Success());
    }

    public ISumUpClient Client { get; } = Substitute.For<ISumUpClient>();

    public ICredentialRepository<SumUpCredential> AccountRepository { get; } = Substitute.For<ICredentialRepository<SumUpCredential>>();

    public ICredentialRepository<SumUpReaderCredential> ReaderRepository { get; } = Substitute.For<ICredentialRepository<SumUpReaderCredential>>();

    public ActiveCredentialProvider<SumUpCredential> Account { get; }

    public ActiveCredentialProvider<SumUpReaderCredential> Readers { get; }

    public SumUpReaderService Service { get; }

    /// <summary>
    /// Creates the fixture with an active account and a short polling setup of three polls.
    /// </summary>
    public static async Task<SumUpReaderServiceFixture> CreateAsync(bool withAccount = true, int maxPolls = 3)
    {
        var options = new SumUpPairingOptions
        {
            PollInterval = TimeSpan.FromMilliseconds(1),
            Timeout = TimeSpan.FromMilliseconds(maxPolls)
        };
        var fixture = new SumUpReaderServiceFixture(options);

        fixture.AccountRepository.GetActiveAsync().Returns(withAccount
            ? Result<SumUpCredential>.Success(BuildAccount())
            : Result<SumUpCredential>.Failure(new Error("NotFound", "none")));
        await fixture.Account.ReloadAsync();

        return fixture;
    }

    public static SumUpCredential BuildAccount() => new()
    {
        Name = "Account",
        Active = true,
        ApiKey = "key",
        MerchantCode = MerchantCode,
        PublicBaseUrl = "https://cloud.example"
    };

    public static SumUpReader BuildReader(string status, string id = "rdr_1", string? name = "Frontdesk") => new()
    {
        Id = id,
        Name = name,
        Status = status,
        Device = new SumUpReaderDevice { Identifier = "serial-1", Model = "solo" }
    };

    public static SumUpReaderCredential BuildSavedReader(string readerId = "rdr_1") => new()
    {
        Name = "Saved",
        ReaderId = readerId,
        MerchantCode = MerchantCode
    };

    public void CreateReturns(SumUpReader reader)
        => Client.CreateReaderAsync(Arg.Any<string>(), Arg.Any<SumUpCreateReaderRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<SumUpReader>.Success(reader));

    public void CreateFails(string code)
        => Client.CreateReaderAsync(Arg.Any<string>(), Arg.Any<SumUpCreateReaderRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<SumUpReader>.Failure(new Error(code, "failed")));

    /// <summary>
    /// Makes consecutive reader requests return the given results; the last one repeats.
    /// </summary>
    public void GetReturns(params Result<SumUpReader>[] results)
        => Client.GetReaderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(results[0], results.Skip(1).ToArray());

    public static Result<SumUpReader> Ok(string status, string id = "rdr_1")
        => Result<SumUpReader>.Success(BuildReader(status, id));

    public static Result<SumUpReader> Failed(string code)
        => Result<SumUpReader>.Failure(new Error(code, "failed"));
}
