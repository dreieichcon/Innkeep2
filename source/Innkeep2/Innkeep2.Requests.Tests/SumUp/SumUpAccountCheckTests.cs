using System.Net;
using Innkeep2.Requests.SumUp;

namespace Innkeep2.Requests.Tests.SumUp;

[TestClass]
public class SumUpAccountCheckTests
{
	private const string GivenKey = "sup_sk_test_given";

	private const string ReaderListJson = """{"items":[{"id":"rdr_1","name":"Front","status":"paired","device":{"identifier":"1","model":"solo"}},{"id":"rdr_2","name":"Back","status":"processing","device":{"identifier":"2","model":"solo"}},{"id":"rdr_3","name":"Old","status":"expired"}]}""";

	private static async Task<SumUpConnectionCheckResult> CheckAsync(SumUpTestHost host)
		=> await host.Checker.CheckAccountAsync(GivenKey, SumUpTestHost.MerchantCode);

	[TestMethod]
	public async Task CheckAccount_SendsGetToDocumentedReaderListUrlWithTheGivenKey()
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.OK, ReaderListJson);

		await CheckAsync(host);

		var request = host.Handler.Requests.Single();
		Assert.AreEqual(HttpMethod.Get, request.Method);
		Assert.AreEqual($"https://api.sumup.com/v0.1/merchants/{SumUpTestHost.MerchantCode}/readers", request.Uri.AbsoluteUri);
		Assert.AreEqual($"Bearer {GivenKey}", request.Authorization);
	}

	[TestMethod]
	public async Task CheckAccount_CountsOnlyPairedReaders()
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.OK, ReaderListJson);

		var result = await CheckAsync(host);

		Assert.AreEqual(SumUpConnectionState.Connected, result.State);
		Assert.AreEqual("Verbunden, 1 Reader gekoppelt", result.Message);
	}

	[TestMethod]
	public async Task CheckAccount_CountsSeveralPairedReadersCaseInsensitive()
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.OK, """{"items":[{"id":"a","status":"paired"},{"id":"b","status":"PAIRED"}]}""");

		var result = await CheckAsync(host);

		Assert.AreEqual("Verbunden, 2 Reader gekoppelt", result.Message);
	}

	[DataRow("""{"items":[]}""")]
	[DataRow("""{}""")]
	[DataRow("""{"items":[{"id":"a","status":"processing"}]}""")]
	[TestMethod]
	public async Task CheckAccount_IsConnectedWithoutPairedReaders(string body)
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.OK, body);

		var result = await CheckAsync(host);

		Assert.AreEqual(SumUpConnectionState.Connected, result.State);
		Assert.AreEqual("Verbunden, es ist noch kein Reader gekoppelt", result.Message);
	}

	[DataRow(HttpStatusCode.Unauthorized)]
	[DataRow(HttpStatusCode.Forbidden)]
	[TestMethod]
	public async Task CheckAccount_ReturnsInvalidKey(HttpStatusCode statusCode)
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(statusCode, """{"title":"Unauthorized"}""");

		var result = await CheckAsync(host);

		Assert.AreEqual(SumUpConnectionState.InvalidKey, result.State);
	}

	[TestMethod]
	public async Task CheckAccount_ReturnsMerchantNotFound()
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.NotFound, """{"title":"Not Found"}""");

		var result = await CheckAsync(host);

		Assert.AreEqual(SumUpConnectionState.MerchantNotFound, result.State);
	}

	[DataRow(HttpStatusCode.InternalServerError)]
	[DataRow(HttpStatusCode.ServiceUnavailable)]
	[DataRow(HttpStatusCode.BadRequest)]
	[TestMethod]
	public async Task CheckAccount_ReturnsUnreachableForOtherErrorStatuses(HttpStatusCode statusCode)
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(statusCode);

		var result = await CheckAsync(host);

		Assert.AreEqual(SumUpConnectionState.Unreachable, result.State);
		StringAssert.Contains(result.Message, ((int)statusCode).ToString());
	}

	[TestMethod]
	public async Task CheckAccount_ReturnsUnreachableWhenTheRequestFails()
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.EnqueueException(new HttpRequestException("connection refused"));

		var result = await CheckAsync(host);

		Assert.AreEqual(SumUpConnectionState.Unreachable, result.State);
	}

	[DataRow("not json")]
	[DataRow("null")]
	[TestMethod]
	public async Task CheckAccount_ReturnsUnreachableForInvalidBodies(string body)
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.OK, body);

		var result = await CheckAsync(host);

		Assert.AreEqual(SumUpConnectionState.Unreachable, result.State);
	}

	[DataRow(null, "MH4H92C7")]
	[DataRow("", "MH4H92C7")]
	[DataRow("  ", "MH4H92C7")]
	[DataRow("key", null)]
	[DataRow("key", "")]
	[DataRow("key", " ")]
	[TestMethod]
	public async Task CheckAccount_ReturnsIncompleteWithoutCallingSumUp(string? apiKey, string? merchantCode)
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();

		var result = await host.Checker.CheckAccountAsync(apiKey, merchantCode);

		Assert.AreEqual(SumUpConnectionState.Incomplete, result.State);
		Assert.IsEmpty(host.Handler.Requests);
	}

	[TestMethod]
	public async Task CheckAccount_UsesTheGivenKeyAndNotTheActiveCredential()
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.OK, ReaderListJson);

		await CheckAsync(host);

		Assert.AreEqual($"Bearer {GivenKey}", host.Handler.Requests.Single().Authorization);
	}

	[TestMethod]
	public async Task CheckAccount_WorksWithoutAnyActiveCredential()
	{
		using var host = await SumUpTestHost.CreateAsync(null);
		host.Handler.Enqueue(HttpStatusCode.OK, ReaderListJson);

		var result = await CheckAsync(host);

		Assert.AreEqual(SumUpConnectionState.Connected, result.State);
	}

	[TestMethod]
	public async Task CheckAccount_EscapesMerchantInUrl()
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.NotFound);

		await host.Checker.CheckAccountAsync(GivenKey, "MH 4/H");

		Assert.AreEqual("https://api.sumup.com/v0.1/merchants/MH%204%2FH/readers", host.Handler.Requests.Single().Uri.AbsoluteUri);
	}
}
