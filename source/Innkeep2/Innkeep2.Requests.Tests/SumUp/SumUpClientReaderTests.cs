using System.Net;
using System.Text.Json;
using Innkeep2.Models.SumUp;

namespace Innkeep2.Requests.Tests.SumUp;

[TestClass]
public class SumUpClientReaderTests
{
	private const string ReaderJson = """{"id":"rdr_3MSAFM23CK82VSTT4BN6RWSQ65","name":"Frontdesk","status":"processing","device":{"identifier":"1234567890","model":"solo"},"metadata":{},"created_at":"2026-10-24T10:00:00Z","updated_at":"2026-10-24T10:00:00Z"}""";

	private static readonly string ReadersUrl = $"https://api.sumup.com/v0.1/merchants/{SumUpTestHost.MerchantCode}/readers";

	private static SumUpCreateReaderRequest BuildCreateRequest() => new() { PairingCode = "4WLFDSBF", Name = "Frontdesk" };

	#region Create

	[TestMethod]
	public async Task CreateReader_SendsPostToDocumentedReadersUrl()
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.OK, ReaderJson);

		await host.Client.CreateReaderAsync(SumUpTestHost.MerchantCode, BuildCreateRequest());

		var request = host.Handler.Requests.Single();
		Assert.AreEqual(HttpMethod.Post, request.Method);
		Assert.AreEqual(ReadersUrl, request.Uri.AbsoluteUri);
		Assert.AreEqual($"Bearer {SumUpTestHost.ApiKey}", request.Authorization);
		Assert.AreEqual("application/json", request.ContentType);
	}

	[TestMethod]
	public async Task CreateReader_SendsPairingCodeAndNameOnly()
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.OK, ReaderJson);

		await host.Client.CreateReaderAsync(SumUpTestHost.MerchantCode, BuildCreateRequest());

		using var body = JsonDocument.Parse(host.Handler.Requests.Single().Body!);
		Assert.AreEqual("4WLFDSBF", body.RootElement.GetProperty("pairing_code").GetString());
		Assert.AreEqual("Frontdesk", body.RootElement.GetProperty("name").GetString());
		Assert.AreEqual(2, body.RootElement.EnumerateObject().Count());
	}

	[DataRow(HttpStatusCode.OK)]
	[DataRow(HttpStatusCode.Created)]
	[TestMethod]
	public async Task CreateReader_ReturnsTheProcessingReader(HttpStatusCode statusCode)
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(statusCode, ReaderJson);

		var result = await host.Client.CreateReaderAsync(SumUpTestHost.MerchantCode, BuildCreateRequest());

		Assert.IsTrue(result.IsSuccess);
		Assert.AreEqual("rdr_3MSAFM23CK82VSTT4BN6RWSQ65", result.Value!.Id);
		Assert.AreEqual("Frontdesk", result.Value.Name);
		Assert.AreEqual("processing", result.Value.Status);
		Assert.AreEqual("solo", result.Value.Device!.Model);
		Assert.AreEqual("1234567890", result.Value.Device.Identifier);
	}

	[DataRow(HttpStatusCode.BadRequest, "Http.400")]
	[DataRow(HttpStatusCode.Unauthorized, "Http.401")]
	[DataRow(HttpStatusCode.NotFound, "Http.404")]
	[DataRow(HttpStatusCode.Conflict, "Http.409")]
	[TestMethod]
	public async Task CreateReader_ReturnsFailureWithTheStatusCodeAsErrorCode(HttpStatusCode statusCode, string expectedCode)
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(statusCode, """{"type":"about:blank","title":"Error","status":400,"detail":"detail"}""");

		var result = await host.Client.CreateReaderAsync(SumUpTestHost.MerchantCode, BuildCreateRequest());

		Assert.IsFalse(result.IsSuccess);
		Assert.AreEqual(expectedCode, result.Error!.Code);
	}

	[TestMethod]
	public async Task CreateReader_WithoutActiveCredential_FailsWithoutCallingSumUp()
	{
		using var host = await SumUpTestHost.CreateAsync(null);

		var result = await host.Client.CreateReaderAsync(SumUpTestHost.MerchantCode, BuildCreateRequest());

		Assert.IsFalse(result.IsSuccess);
		Assert.IsEmpty(host.Handler.Requests);
	}

	#endregion

	#region Get

	[TestMethod]
	public async Task GetReader_SendsGetToDocumentedReaderUrl()
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.OK, ReaderJson);

		await host.Client.GetReaderAsync(SumUpTestHost.MerchantCode, SumUpTestHost.ReaderId);

		var request = host.Handler.Requests.Single();
		Assert.AreEqual(HttpMethod.Get, request.Method);
		Assert.AreEqual($"{ReadersUrl}/{SumUpTestHost.ReaderId}", request.Uri.AbsoluteUri);
		Assert.AreEqual($"Bearer {SumUpTestHost.ApiKey}", request.Authorization);
	}

	[DataRow("processing")]
	[DataRow("paired")]
	[DataRow("expired")]
	[DataRow("unknown")]
	[TestMethod]
	public async Task GetReader_ReturnsTheDocumentedReaderStatus(string status)
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.OK, ReaderJson.Replace("\"processing\"", $"\"{status}\""));

		var result = await host.Client.GetReaderAsync(SumUpTestHost.MerchantCode, SumUpTestHost.ReaderId);

		Assert.IsTrue(result.IsSuccess);
		Assert.AreEqual(status, result.Value!.Status);
	}

	[TestMethod]
	public async Task GetReader_ReturnsFailureForNotFound()
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.NotFound, """{"title":"Not Found"}""");

		var result = await host.Client.GetReaderAsync(SumUpTestHost.MerchantCode, SumUpTestHost.ReaderId);

		Assert.IsFalse(result.IsSuccess);
		Assert.AreEqual("Http.404", result.Error!.Code);
	}

	[TestMethod]
	public async Task GetReader_EscapesMerchantAndReaderInUrl()
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.NotFound);

		await host.Client.GetReaderAsync("MH 4/H", "rdr/../x");

		Assert.AreEqual("https://api.sumup.com/v0.1/merchants/MH%204%2FH/readers/rdr%2F..%2Fx", host.Handler.Requests.Single().Uri.AbsoluteUri);
	}

	[TestMethod]
	public async Task GetReader_WithoutActiveCredential_FailsWithoutCallingSumUp()
	{
		using var host = await SumUpTestHost.CreateAsync(null);

		var result = await host.Client.GetReaderAsync(SumUpTestHost.MerchantCode, SumUpTestHost.ReaderId);

		Assert.IsFalse(result.IsSuccess);
		Assert.IsEmpty(host.Handler.Requests);
	}

	#endregion

	#region List

	[TestMethod]
	public async Task ListReaders_SendsGetToDocumentedReadersUrl()
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.OK, """{"items":[]}""");

		await host.Client.ListReadersAsync(SumUpTestHost.MerchantCode);

		var request = host.Handler.Requests.Single();
		Assert.AreEqual(HttpMethod.Get, request.Method);
		Assert.AreEqual(ReadersUrl, request.Uri.AbsoluteUri);
		Assert.AreEqual($"Bearer {SumUpTestHost.ApiKey}", request.Authorization);
	}

	[TestMethod]
	public async Task ListReaders_ReturnsAllReaders()
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.OK, $$"""{"items":[{{ReaderJson}},{{ReaderJson.Replace("rdr_3MSAFM23CK82VSTT4BN6RWSQ65", "rdr_other").Replace("processing", "paired")}}]}""");

		var result = await host.Client.ListReadersAsync(SumUpTestHost.MerchantCode);

		Assert.IsTrue(result.IsSuccess);
		Assert.HasCount(2, result.Value!.Items);
		Assert.AreEqual("rdr_other", result.Value.Items[1].Id);
		Assert.AreEqual("paired", result.Value.Items[1].Status);
	}

	[TestMethod]
	public async Task ListReaders_ReturnsAnEmptyListWithoutReaders()
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.OK, """{"items":[]}""");

		var result = await host.Client.ListReadersAsync(SumUpTestHost.MerchantCode);

		Assert.IsTrue(result.IsSuccess);
		Assert.IsEmpty(result.Value!.Items);
	}

	[TestMethod]
	public async Task ListReaders_ReturnsFailureForUnauthorized()
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.Unauthorized, """{"title":"Unauthorized"}""");

		var result = await host.Client.ListReadersAsync(SumUpTestHost.MerchantCode);

		Assert.IsFalse(result.IsSuccess);
		Assert.AreEqual("Http.401", result.Error!.Code);
	}

	[TestMethod]
	public async Task ListReaders_WithoutActiveCredential_FailsWithoutCallingSumUp()
	{
		using var host = await SumUpTestHost.CreateAsync(null);

		var result = await host.Client.ListReadersAsync(SumUpTestHost.MerchantCode);

		Assert.IsFalse(result.IsSuccess);
		Assert.IsEmpty(host.Handler.Requests);
	}

	#endregion

	#region Delete

	[TestMethod]
	public async Task DeleteReader_SendsDeleteToDocumentedReaderUrl()
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.NoContent);

		await host.Client.DeleteReaderAsync(SumUpTestHost.MerchantCode, SumUpTestHost.ReaderId);

		var request = host.Handler.Requests.Single();
		Assert.AreEqual(HttpMethod.Delete, request.Method);
		Assert.AreEqual($"{ReadersUrl}/{SumUpTestHost.ReaderId}", request.Uri.AbsoluteUri);
		Assert.AreEqual($"Bearer {SumUpTestHost.ApiKey}", request.Authorization);
	}

	[DataRow(HttpStatusCode.OK)]
	[DataRow(HttpStatusCode.NoContent)]
	[TestMethod]
	public async Task DeleteReader_ReturnsSuccessForEmptyResponse(HttpStatusCode statusCode)
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(statusCode);

		var result = await host.Client.DeleteReaderAsync(SumUpTestHost.MerchantCode, SumUpTestHost.ReaderId);

		Assert.IsTrue(result.IsSuccess);
	}

	[TestMethod]
	public async Task DeleteReader_ReturnsFailureForNotFound()
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.NotFound, """{"title":"The requested Reader resource does not exist."}""");

		var result = await host.Client.DeleteReaderAsync(SumUpTestHost.MerchantCode, SumUpTestHost.ReaderId);

		Assert.IsFalse(result.IsSuccess);
		Assert.AreEqual("Http.404", result.Error!.Code);
	}

	[TestMethod]
	public async Task DeleteReader_WithoutActiveCredential_FailsWithoutCallingSumUp()
	{
		using var host = await SumUpTestHost.CreateAsync(null);

		var result = await host.Client.DeleteReaderAsync(SumUpTestHost.MerchantCode, SumUpTestHost.ReaderId);

		Assert.IsFalse(result.IsSuccess);
		Assert.IsEmpty(host.Handler.Requests);
	}

	#endregion
}
