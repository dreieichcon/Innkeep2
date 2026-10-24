using Demolite.Core.Models;
using Innkeep2.CredentialsDb;
using Innkeep2.CredentialsDb.Interfaces;
using Innkeep2.CredentialsDb.Models;
using Innkeep2.Requests.SumUp;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Innkeep2.Requests.Tests.SumUp;

public sealed class SumUpTestHost : IDisposable
{
	public const string MerchantCode = "MH4H92C7";
	public const string ReaderId = "rdr_3MSAFM23CK82VSTT4BN6RWSQ65";
	public const string ApiKey = "sup_sk_test_key";

	private readonly ServiceProvider _provider;
	private readonly ICredentialRepository<SumUpCredential> _repository = Substitute.For<ICredentialRepository<SumUpCredential>>();

	private SumUpTestHost()
	{
		var services = new ServiceCollection();

		services.AddLogging();
		services.AddSingleton(_repository);
		services.AddSingleton<ActiveCredentialProvider<SumUpCredential>>();
		services.AddSumUpClients();
		services.AddHttpClient<SumUpClient>().ConfigurePrimaryHttpMessageHandler(() => Handler);

		_provider = services.BuildServiceProvider();
	}

	public StubHttpMessageHandler Handler { get; } = new();

	public ServiceProvider Provider => _provider;

	public SumUpClient Client => _provider.GetRequiredService<SumUpClient>();

	public ActiveCredentialProvider<SumUpCredential> Credentials => _provider.GetRequiredService<ActiveCredentialProvider<SumUpCredential>>();

	/// <summary>
	/// Creates a host whose repository returns the given credential as the active one, or none.
	/// </summary>
	public static async Task<SumUpTestHost> CreateAsync(SumUpCredential? active)
	{
		var host = new SumUpTestHost();
		await host.SetActiveCredentialAsync(active);

		return host;
	}

	/// <summary>
	/// Creates a host with the default test credential active.
	/// </summary>
	public static Task<SumUpTestHost> CreateWithCredentialAsync()
		=> CreateAsync(BuildCredential(ApiKey));

	public static SumUpCredential BuildCredential(string apiKey) => new()
	{
		Name = "Test",
		Active = true,
		ApiKey = apiKey,
		MerchantCode = MerchantCode,
		ReaderId = ReaderId,
		PublicBaseUrl = "https://cloud.example"
	};

	/// <summary>
	/// Changes the active credential in the repository and reloads the provider.
	/// </summary>
	public async Task SetActiveCredentialAsync(SumUpCredential? active)
	{
		_repository.GetActiveAsync().Returns(active is null
			? Result<SumUpCredential>.Failure(new Error("NotFound", "No active credential."))
			: Result<SumUpCredential>.Success(active));

		await Credentials.ReloadAsync();
	}

	public void Dispose()
		=> _provider.Dispose();
}
