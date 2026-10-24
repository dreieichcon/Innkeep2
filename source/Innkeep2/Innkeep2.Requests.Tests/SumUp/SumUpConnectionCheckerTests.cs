using System.Net;
using Innkeep2.Requests.SumUp;

namespace Innkeep2.Requests.Tests.SumUp;

[TestClass]
public class SumUpConnectionCheckerTests
{
	private const string GivenKey = "sup_sk_test_given";

	private const string ReaderJson = """{"id":"rdr_3MSAFM23CK82VSTT4BN6RWSQ65","name":"Frontdesk","status":"paired","device":{"identifier":"1234","model":"solo"},"metadata":{},"created_at":"2026-10-24T10:00:00Z","updated_at":"2026-10-24T10:00:00Z"}""";

	private const string OnlineStatusJson = """{"data":{"battery_level":80,"connection_type":"Wi-Fi","firmware_version":"3.3.3.21","last_activity":"2026-10-24T10:00:00Z","state":"IDLE","status":"ONLINE"}}""";

	private const string OfflineStatusJson = """{"data":{"state":"IDLE","status":"OFFLINE"}}""";

	private static async Task<SumUpConnectionCheckResult> CheckAsync(SumUpTestHost host)
		=> await host.Checker.CheckReaderAsync(GivenKey, SumUpTestHost.MerchantCode, SumUpTestHost.ReaderId);

	[TestMethod]
	public async Task Check_SendsGetToDocumentedReaderUrlWithTheGivenKey()
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.OK, ReaderJson);
		host.Handler.Enqueue(HttpStatusCode.OK, OnlineStatusJson);

		await CheckAsync(host);

		var request = host.Handler.Requests[0];
		Assert.AreEqual(HttpMethod.Get, request.Method);
		Assert.AreEqual($"https://api.sumup.com/v0.1/merchants/{SumUpTestHost.MerchantCode}/readers/{SumUpTestHost.ReaderId}", request.Uri.AbsoluteUri);
		Assert.AreEqual($"Bearer {GivenKey}", request.Authorization);
	}

	[TestMethod]
	public async Task Check_RequestsTheDocumentedStatusEndpointWithTheGivenKey()
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.OK, ReaderJson);
		host.Handler.Enqueue(HttpStatusCode.OK, OnlineStatusJson);

		await CheckAsync(host);

		Assert.HasCount(2, host.Handler.Requests);
		Assert.AreEqual($"https://api.sumup.com/v0.1/merchants/{SumUpTestHost.MerchantCode}/readers/{SumUpTestHost.ReaderId}/status", host.Handler.Requests[1].Uri.AbsoluteUri);
		Assert.AreEqual($"Bearer {GivenKey}", host.Handler.Requests[1].Authorization);
	}

	[TestMethod]
	public async Task Check_ReturnsConnectedAndOnlineForPairedOnlineReader()
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.OK, ReaderJson);
		host.Handler.Enqueue(HttpStatusCode.OK, OnlineStatusJson);

		var result = await CheckAsync(host);

		Assert.AreEqual(SumUpConnectionState.Connected, result.State);
		Assert.AreEqual("Frontdesk", result.ReaderName);
		Assert.AreEqual("solo", result.DeviceModel);
		Assert.IsTrue(result.ReaderOnline);
		Assert.AreEqual("IDLE", result.ReaderState);
		Assert.AreEqual("Verbunden mit 'Frontdesk' (solo), online", result.Message);
	}

	[TestMethod]
	public async Task Check_ReturnsConnectedButOfflineForOfflineReader()
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.OK, ReaderJson);
		host.Handler.Enqueue(HttpStatusCode.OK, OfflineStatusJson);

		var result = await CheckAsync(host);

		Assert.AreEqual(SumUpConnectionState.Connected, result.State);
		Assert.IsFalse(result.ReaderOnline);
		Assert.AreEqual("Verbunden mit 'Frontdesk' (solo), offline", result.Message);
	}

	[DataRow(HttpStatusCode.NotFound)]
	[DataRow(HttpStatusCode.BadRequest)]
	[DataRow(HttpStatusCode.InternalServerError)]
	[TestMethod]
	public async Task Check_StaysConnectedWithUnknownOnlineStateWhenStatusEndpointFails(HttpStatusCode statusCode)
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.OK, ReaderJson);
		host.Handler.Enqueue(statusCode, """{"title":"error"}""");

		var result = await CheckAsync(host);

		Assert.AreEqual(SumUpConnectionState.Connected, result.State);
		Assert.IsNull(result.ReaderOnline);
		Assert.AreEqual("Verbunden mit 'Frontdesk' (solo), Online-Status unbekannt", result.Message);
	}

	[TestMethod]
	public async Task Check_StaysConnectedWithUnknownOnlineStateWhenStatusBodyIsInvalid()
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.OK, ReaderJson);
		host.Handler.Enqueue(HttpStatusCode.OK, "not json");

		var result = await CheckAsync(host);

		Assert.AreEqual(SumUpConnectionState.Connected, result.State);
		Assert.IsNull(result.ReaderOnline);
	}

	[TestMethod]
	public async Task Check_AcceptsPairedStatusCaseInsensitive()
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.OK, ReaderJson.Replace("\"paired\"", "\"PAIRED\""));
		host.Handler.Enqueue(HttpStatusCode.OK, OnlineStatusJson);

		var result = await CheckAsync(host);

		Assert.AreEqual(SumUpConnectionState.Connected, result.State);
	}

	[TestMethod]
	public async Task Check_ShowsTheVirtualSoloModel()
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.OK, ReaderJson.Replace("\"solo\"", "\"virtual-solo\""));
		host.Handler.Enqueue(HttpStatusCode.OK, OnlineStatusJson);

		var result = await CheckAsync(host);

		Assert.AreEqual("virtual-solo", result.DeviceModel);
		StringAssert.Contains(result.Message, "virtual-solo");
	}

	[DataRow(HttpStatusCode.Unauthorized)]
	[DataRow(HttpStatusCode.Forbidden)]
	[TestMethod]
	public async Task Check_ReturnsInvalidKeyWithoutCallingTheStatusEndpoint(HttpStatusCode statusCode)
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(statusCode, """{"title":"Unauthorized"}""");

		var result = await CheckAsync(host);

		Assert.AreEqual(SumUpConnectionState.InvalidKey, result.State);
		Assert.HasCount(1, host.Handler.Requests);
	}

	[TestMethod]
	public async Task Check_ReturnsReaderNotFoundForNotFound()
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.NotFound, """{"title":"Not Found"}""");

		var result = await CheckAsync(host);

		Assert.AreEqual(SumUpConnectionState.ReaderNotFound, result.State);
		Assert.HasCount(1, host.Handler.Requests);
	}

	[DataRow("processing")]
	[DataRow("expired")]
	[DataRow("unknown")]
	[TestMethod]
	public async Task Check_ReturnsReaderNotPairedForOtherReaderStatuses(string readerStatus)
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.OK, ReaderJson.Replace("\"paired\"", $"\"{readerStatus}\""));

		var result = await CheckAsync(host);

		Assert.AreEqual(SumUpConnectionState.ReaderNotPaired, result.State);
		StringAssert.Contains(result.Message, readerStatus);
		Assert.HasCount(1, host.Handler.Requests);
	}

	[DataRow(HttpStatusCode.InternalServerError)]
	[DataRow(HttpStatusCode.ServiceUnavailable)]
	[DataRow(HttpStatusCode.BadRequest)]
	[TestMethod]
	public async Task Check_ReturnsUnreachableForOtherErrorStatuses(HttpStatusCode statusCode)
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(statusCode);

		var result = await CheckAsync(host);

		Assert.AreEqual(SumUpConnectionState.Unreachable, result.State);
		StringAssert.Contains(result.Message, ((int)statusCode).ToString());
	}

	[TestMethod]
	public async Task Check_ReturnsUnreachableWhenTheRequestFails()
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.EnqueueException(new HttpRequestException("connection refused"));

		var result = await CheckAsync(host);

		Assert.AreEqual(SumUpConnectionState.Unreachable, result.State);
	}

	[TestMethod]
	public async Task Check_ReturnsUnreachableWhenTheReaderBodyIsInvalid()
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.OK, "not json");

		var result = await CheckAsync(host);

		Assert.AreEqual(SumUpConnectionState.Unreachable, result.State);
	}

	[TestMethod]
	public async Task Check_ReturnsUnreachableWhenTheReaderBodyIsNull()
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.OK, "null");

		var result = await CheckAsync(host);

		Assert.AreEqual(SumUpConnectionState.Unreachable, result.State);
	}

	[DataRow(null, "MH4H92C7", "rdr_1")]
	[DataRow("", "MH4H92C7", "rdr_1")]
	[DataRow("  ", "MH4H92C7", "rdr_1")]
	[DataRow("key", null, "rdr_1")]
	[DataRow("key", "", "rdr_1")]
	[DataRow("key", "MH4H92C7", null)]
	[DataRow("key", "MH4H92C7", " ")]
	[TestMethod]
	public async Task Check_ReturnsIncompleteWithoutCallingSumUp(string? apiKey, string? merchantCode, string? readerId)
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();

		var result = await host.Checker.CheckReaderAsync(apiKey, merchantCode, readerId);

		Assert.AreEqual(SumUpConnectionState.Incomplete, result.State);
		Assert.IsEmpty(host.Handler.Requests);
	}

	[TestMethod]
	public async Task Check_UsesTheGivenKeyAndNotTheActiveCredential()
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.OK, ReaderJson);
		host.Handler.Enqueue(HttpStatusCode.OK, OnlineStatusJson);

		await CheckAsync(host);

		Assert.AreNotEqual($"Bearer {SumUpTestHost.ApiKey}", host.Handler.Requests[0].Authorization);
		Assert.AreEqual($"Bearer {GivenKey}", host.Handler.Requests[0].Authorization);
	}

	[TestMethod]
	public async Task Check_WorksWithoutAnyActiveCredential()
	{
		using var host = await SumUpTestHost.CreateAsync(null);
		host.Handler.Enqueue(HttpStatusCode.OK, ReaderJson);
		host.Handler.Enqueue(HttpStatusCode.OK, OnlineStatusJson);

		var result = await CheckAsync(host);

		Assert.AreEqual(SumUpConnectionState.Connected, result.State);
	}

	[TestMethod]
	public async Task Check_EscapesMerchantAndReaderInUrl()
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.NotFound);

		await host.Checker.CheckReaderAsync(GivenKey, "MH 4/H", "rdr/../x");

		Assert.AreEqual("https://api.sumup.com/v0.1/merchants/MH%204%2FH/readers/rdr%2F..%2Fx", host.Handler.Requests.Single().Uri.AbsoluteUri);
	}
}
