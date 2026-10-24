using System.Net;

namespace Innkeep2.Requests.Tests.SumUp;

[TestClass]
public class SumUpClientGetTransactionTests
{
	private const string ClientTransactionId = "urn:sumup:pos:sale:MNKKNGST:1D4E3B2D-111D-48D7-9AF0-832DAEF63DD7;2";

	private static string BuildTransaction(string status)
		=> """{"id":"tr_1","status":"STATUS","amount":10.1,"currency":"EUR","transaction_code":"TEENSK4W2K","client_transaction_id":"CLIENT_ID","timestamp":"2026-10-24T10:00:00Z","unknown_field":{"a":1}}"""
			.Replace("STATUS", status)
			.Replace("CLIENT_ID", ClientTransactionId);

	[TestMethod]
	public async Task GetTransaction_SendsGetWithEscapedClientTransactionId()
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.OK, BuildTransaction("SUCCESSFUL"));

		await host.Client.GetTransactionAsync(SumUpTestHost.MerchantCode, ClientTransactionId);

		var request = host.Handler.Requests.Single();
		Assert.AreEqual(HttpMethod.Get, request.Method);
		Assert.AreEqual(
			$"https://api.sumup.com/v2.1/merchants/{SumUpTestHost.MerchantCode}/transactions?client_transaction_id=urn%3Asumup%3Apos%3Asale%3AMNKKNGST%3A1D4E3B2D-111D-48D7-9AF0-832DAEF63DD7%3B2",
			request.Uri.AbsoluteUri);
	}

	[TestMethod]
	public async Task GetTransaction_SendsApiKeyAsBearerToken()
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.OK, BuildTransaction("SUCCESSFUL"));

		await host.Client.GetTransactionAsync(SumUpTestHost.MerchantCode, ClientTransactionId);

		Assert.AreEqual($"Bearer {SumUpTestHost.ApiKey}", host.Handler.Requests.Single().Authorization);
	}

	[DataRow("SUCCESSFUL")]
	[DataRow("PENDING")]
	[DataRow("FAILED")]
	[DataRow("CANCELLED")]
	[DataRow("REFUNDED")]
	[TestMethod]
	public async Task GetTransaction_ReturnsDocumentedStatus(string status)
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.OK, BuildTransaction(status));

		var result = await host.Client.GetTransactionAsync(SumUpTestHost.MerchantCode, ClientTransactionId);

		Assert.IsTrue(result.IsSuccess);
		Assert.AreEqual(status, result.Value!.Status);
	}

	[TestMethod]
	public async Task GetTransaction_ReturnsAmountInMajorUnitsAndTransactionDetails()
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.OK, BuildTransaction("SUCCESSFUL"));

		var result = await host.Client.GetTransactionAsync(SumUpTestHost.MerchantCode, ClientTransactionId);

		Assert.AreEqual(10.1m, result.Value!.Amount);
		Assert.AreEqual("EUR", result.Value.Currency);
		Assert.AreEqual("TEENSK4W2K", result.Value.TransactionCode);
		Assert.AreEqual(ClientTransactionId, result.Value.ClientTransactionId);
	}

	[DataRow(HttpStatusCode.BadRequest)]
	[DataRow(HttpStatusCode.Unauthorized)]
	[DataRow(HttpStatusCode.NotFound)]
	[DataRow(HttpStatusCode.InternalServerError)]
	[TestMethod]
	public async Task GetTransaction_ReturnsFailureForErrorStatus(HttpStatusCode status)
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(status, """{"type":"about:blank","title":"Error","status":404,"detail":"Not found"}""");

		var result = await host.Client.GetTransactionAsync(SumUpTestHost.MerchantCode, ClientTransactionId);

		Assert.IsFalse(result.IsSuccess);
		Assert.IsNotNull(result.Error);
	}

	[TestMethod]
	public async Task GetTransaction_WithoutActiveCredential_FailsWithoutCallingSumUp()
	{
		using var host = await SumUpTestHost.CreateAsync(null);

		var result = await host.Client.GetTransactionAsync(SumUpTestHost.MerchantCode, ClientTransactionId);

		Assert.IsFalse(result.IsSuccess);
		Assert.IsEmpty(host.Handler.Requests);
	}

	[TestMethod]
	public async Task GetTransaction_ReturnsTheTransactionIdNeededForARefund()
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.OK, """{"id":"4ffb8dfc-7f2b-413d-a497-2ad00766585e","status":"SUCCESSFUL","amount":10.1,"currency":"EUR","transaction_code":"TEENSK4W2K"}""");

		var result = await host.Client.GetTransactionAsync(SumUpTestHost.MerchantCode, ClientTransactionId);

		Assert.AreEqual("4ffb8dfc-7f2b-413d-a497-2ad00766585e", result.Value!.Id);
	}

	[TestMethod]
	public async Task GetTransaction_ReturnsTheRefundedAmount()
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.OK, """{"id":"abc","status":"SUCCESSFUL","amount":10.1,"refunded_amount":4.5}""");

		var result = await host.Client.GetTransactionAsync(SumUpTestHost.MerchantCode, ClientTransactionId);

		Assert.AreEqual(4.5m, result.Value!.RefundedAmount);
	}

	[TestMethod]
	public async Task GetTransaction_HasNoRefundedAmountOrIdWhenSumUpOmitsThem()
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.OK, """{"status":"SUCCESSFUL"}""");

		var result = await host.Client.GetTransactionAsync(SumUpTestHost.MerchantCode, ClientTransactionId);

		Assert.IsNull(result.Value!.Id);
		Assert.IsNull(result.Value.RefundedAmount);
	}
}
