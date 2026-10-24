using System.Net;
using System.Text.Json;
using Innkeep2.Requests.SumUp;
using Microsoft.Extensions.DependencyInjection;

namespace Innkeep2.Requests.Tests.SumUp;

[TestClass]
public class SumUpClientRefundTests
{
	private const string TransactionId = "4ffb8dfc-7f2b-413d-a497-2ad00766585e";

	private static readonly string RefundUrl = $"https://api.sumup.com/v1.0/merchants/{SumUpTestHost.MerchantCode}/payments/{TransactionId}/refunds";

	[TestMethod]
	public async Task Refund_SendsPostToTheDocumentedRefundUrlWithTheApiKey()
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.OK, "{}");

		await host.Client.RefundTransactionAsync(SumUpTestHost.MerchantCode, TransactionId);

		var request = host.Handler.Requests.Single();
		Assert.AreEqual(HttpMethod.Post, request.Method);
		Assert.AreEqual(RefundUrl, request.Uri.AbsoluteUri);
		Assert.AreEqual($"Bearer {SumUpTestHost.ApiKey}", request.Authorization);
	}

	[TestMethod]
	public async Task Refund_WithoutAnAmountSendsAnEmptyJsonObjectForAFullRefund()
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.OK, "{}");

		await host.Client.RefundTransactionAsync(SumUpTestHost.MerchantCode, TransactionId);

		var request = host.Handler.Requests.Single();
		Assert.AreEqual("application/json", request.ContentType);
		using var body = JsonDocument.Parse(request.Body!);
		Assert.AreEqual(JsonValueKind.Object, body.RootElement.ValueKind);
		Assert.IsFalse(body.RootElement.TryGetProperty("amount", out _));
	}

	[DataRow("5", 5.0)]
	[DataRow("12.5", 12.5)]
	[DataRow("0.01", 0.01)]
	[TestMethod]
	public async Task Refund_WithAnAmountSendsItInMajorUnitsAsANumber(string amount, double expected)
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.OK, "{}");

		await host.Client.RefundTransactionAsync(SumUpTestHost.MerchantCode, TransactionId, decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture));

		using var body = JsonDocument.Parse(host.Handler.Requests.Single().Body!);
		var sent = body.RootElement.GetProperty("amount");
		Assert.AreEqual(JsonValueKind.Number, sent.ValueKind);
		Assert.AreEqual(expected, sent.GetDouble(), 0.0000001);
	}

	[DataRow(HttpStatusCode.OK)]
	[DataRow(HttpStatusCode.NoContent)]
	[DataRow(HttpStatusCode.Accepted)]
	[TestMethod]
	public async Task Refund_ReturnsSuccessForASuccessStatus(HttpStatusCode statusCode)
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(statusCode, statusCode == HttpStatusCode.OK ? "{}" : null);

		var result = await host.Client.RefundTransactionAsync(SumUpTestHost.MerchantCode, TransactionId);

		Assert.IsTrue(result.IsSuccess);
	}

	[DataRow(HttpStatusCode.BadRequest, "Http.400")]
	[DataRow(HttpStatusCode.Forbidden, "Http.403")]
	[DataRow(HttpStatusCode.NotFound, "Http.404")]
	[DataRow(HttpStatusCode.Conflict, "Http.409")]
	[DataRow(HttpStatusCode.UnprocessableEntity, "Http.422")]
	[DataRow(HttpStatusCode.InternalServerError, "Http.500")]
	[TestMethod]
	public async Task Refund_ReturnsFailureWithTheStatusCodeAsErrorCode(HttpStatusCode statusCode, string expectedCode)
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(statusCode, """{"title":"error","detail":"amount_too_high","max_refundable_amount":4.5}""");

		var result = await host.Client.RefundTransactionAsync(SumUpTestHost.MerchantCode, TransactionId);

		Assert.IsFalse(result.IsSuccess);
		Assert.AreEqual(expectedCode, result.Error!.Code);
		StringAssert.Contains(result.Error.Message, "amount_too_high");
	}

	[TestMethod]
	public async Task Refund_ReturnsFailureForANetworkError()
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.EnqueueException(new HttpRequestException("connection refused"));

		var result = await host.Client.RefundTransactionAsync(SumUpTestHost.MerchantCode, TransactionId);

		Assert.IsFalse(result.IsSuccess);
		Assert.AreEqual("Http.RequestFailed", result.Error!.Code);
	}

	[TestMethod]
	public async Task Refund_EscapesMerchantAndTransactionIdInTheUrl()
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.NotFound);

		await host.Client.RefundTransactionAsync("MH 4/H", "id/../x");

		Assert.AreEqual("https://api.sumup.com/v1.0/merchants/MH%204%2FH/payments/id%2F..%2Fx/refunds", host.Handler.Requests.Single().Uri.AbsoluteUri);
	}

	[TestMethod]
	public async Task Refund_WithoutActiveCredential_FailsWithoutCallingSumUp()
	{
		using var host = await SumUpTestHost.CreateAsync(null);

		var result = await host.Client.RefundTransactionAsync(SumUpTestHost.MerchantCode, TransactionId);

		Assert.IsFalse(result.IsSuccess);
		Assert.IsEmpty(host.Handler.Requests);
	}

	[TestMethod]
	public async Task Refund_IsAvailableThroughTheInterface()
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.OK, "{}");

		var result = await host.Provider.GetRequiredService<ISumUpClient>()
			.RefundTransactionAsync(SumUpTestHost.MerchantCode, TransactionId, 3m);

		Assert.IsTrue(result.IsSuccess);
		Assert.AreEqual(RefundUrl, host.Handler.Requests.Single().Uri.AbsoluteUri);
	}
}
