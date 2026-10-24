using System.Net;
using System.Text.Json;
using Innkeep2.Models.SumUp;

namespace Innkeep2.Requests.Tests.SumUp;

[TestClass]
public class SumUpClientCreateCheckoutTests
{
	private const string CheckoutResponse = """{"data":{"client_transaction_id":"4e425463-3e1b-431d-83fa-1e51c2925e99","checkout_id":"chk_1"}}""";

	private static SumUpCheckoutRequest BuildRequest(string? returnUrl = "https://cloud.example/webhooks/sumup/token", SumUpAffiliate? affiliate = null, string? description = null) => new()
	{
		TotalAmount = SumUpMoney.FromDecimal(15m, "EUR"),
		ReturnUrl = returnUrl,
		Affiliate = affiliate,
		Description = description
	};

	private static async Task<(SumUpTestHost Host, Demolite.Core.Models.Result<SumUpCheckoutResponse> Result)> SendAsync(SumUpCheckoutRequest request, HttpStatusCode status = HttpStatusCode.OK, string? body = CheckoutResponse)
	{
		var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(status, body);

		var result = await host.Client.CreateCheckoutAsync(SumUpTestHost.MerchantCode, SumUpTestHost.ReaderId, request);

		return (host, result);
	}

	[TestMethod]
	public async Task CreateCheckout_SendsPostToDocumentedCheckoutUrl()
	{
		var (host, _) = await SendAsync(BuildRequest());
		using var scope = host;

		var request = host.Handler.Requests.Single();
		Assert.AreEqual(HttpMethod.Post, request.Method);
		Assert.AreEqual($"https://api.sumup.com/v0.1/merchants/{SumUpTestHost.MerchantCode}/readers/{SumUpTestHost.ReaderId}/checkout", request.Uri.AbsoluteUri);
	}

	[TestMethod]
	public async Task CreateCheckout_SendsApiKeyAsBearerToken()
	{
		var (host, _) = await SendAsync(BuildRequest());
		using var scope = host;

		Assert.AreEqual($"Bearer {SumUpTestHost.ApiKey}", host.Handler.Requests.Single().Authorization);
	}

	[TestMethod]
	public async Task CreateCheckout_SendsJsonBody()
	{
		var (host, _) = await SendAsync(BuildRequest());
		using var scope = host;

		Assert.AreEqual("application/json", host.Handler.Requests.Single().ContentType);
	}

	[TestMethod]
	public async Task CreateCheckout_BodyContainsTotalAmountInMinorUnits()
	{
		var (host, _) = await SendAsync(BuildRequest());
		using var scope = host;

		using var body = JsonDocument.Parse(host.Handler.Requests.Single().Body!);
		var totalAmount = body.RootElement.GetProperty("total_amount");
		Assert.AreEqual("EUR", totalAmount.GetProperty("currency").GetString());
		Assert.AreEqual(2, totalAmount.GetProperty("minor_unit").GetInt32());
		Assert.AreEqual(JsonValueKind.Number, totalAmount.GetProperty("value").ValueKind);
		Assert.AreEqual(1500, totalAmount.GetProperty("value").GetInt64());
	}

	[TestMethod]
	public async Task CreateCheckout_BodyContainsReturnUrl()
	{
		var (host, _) = await SendAsync(BuildRequest("https://cloud.example/webhooks/sumup/abc"));
		using var scope = host;

		using var body = JsonDocument.Parse(host.Handler.Requests.Single().Body!);
		Assert.AreEqual("https://cloud.example/webhooks/sumup/abc", body.RootElement.GetProperty("return_url").GetString());
	}

	[TestMethod]
	public async Task CreateCheckout_OmitsOptionalFieldsWhenNotSet()
	{
		var (host, _) = await SendAsync(BuildRequest(returnUrl: null));
		using var scope = host;

		using var body = JsonDocument.Parse(host.Handler.Requests.Single().Body!);
		Assert.IsFalse(body.RootElement.TryGetProperty("affiliate", out _));
		Assert.IsFalse(body.RootElement.TryGetProperty("description", out _));
		Assert.IsFalse(body.RootElement.TryGetProperty("return_url", out _));
	}

	[TestMethod]
	public async Task CreateCheckout_BodyContainsAffiliateWhenSet()
	{
		var affiliate = new SumUpAffiliate { AppId = "app.example", Key = "affiliate-key", ForeignTransactionId = "order-1" };
		var (host, _) = await SendAsync(BuildRequest(affiliate: affiliate));
		using var scope = host;

		using var body = JsonDocument.Parse(host.Handler.Requests.Single().Body!);
		var sent = body.RootElement.GetProperty("affiliate");
		Assert.AreEqual("app.example", sent.GetProperty("app_id").GetString());
		Assert.AreEqual("affiliate-key", sent.GetProperty("key").GetString());
		Assert.AreEqual("order-1", sent.GetProperty("foreign_transaction_id").GetString());
	}

	[TestMethod]
	public async Task CreateCheckout_BodyContainsDescriptionWhenSet()
	{
		var (host, _) = await SendAsync(BuildRequest(description: "Kasse 1"));
		using var scope = host;

		using var body = JsonDocument.Parse(host.Handler.Requests.Single().Body!);
		Assert.AreEqual("Kasse 1", body.RootElement.GetProperty("description").GetString());
	}

	[TestMethod]
	public async Task CreateCheckout_ReturnsClientTransactionIdAndCheckoutId()
	{
		var (host, result) = await SendAsync(BuildRequest());
		using var scope = host;

		Assert.IsTrue(result.IsSuccess);
		Assert.AreEqual("4e425463-3e1b-431d-83fa-1e51c2925e99", result.Value!.Data.ClientTransactionId);
		Assert.AreEqual("chk_1", result.Value.Data.CheckoutId);
	}

	[TestMethod]
	public async Task CreateCheckout_AcceptsResponseWithoutCheckoutId()
	{
		var (host, result) = await SendAsync(BuildRequest(), body: """{"data":{"client_transaction_id":"abc"}}""");
		using var scope = host;

		Assert.IsTrue(result.IsSuccess);
		Assert.AreEqual("abc", result.Value!.Data.ClientTransactionId);
		Assert.IsNull(result.Value.Data.CheckoutId);
	}

	[DataRow(HttpStatusCode.BadRequest)]
	[DataRow(HttpStatusCode.Unauthorized)]
	[DataRow(HttpStatusCode.NotFound)]
	[DataRow(HttpStatusCode.UnprocessableEntity)]
	[DataRow(HttpStatusCode.InternalServerError)]
	[TestMethod]
	public async Task CreateCheckout_ReturnsFailureForErrorStatus(HttpStatusCode status)
	{
		const string problem = """{"type":"https://developer.sumup.com/problem/not-found","title":"Not Found","status":404,"detail":"Reader not found"}""";
		var (host, result) = await SendAsync(BuildRequest(), status, problem);
		using var scope = host;

		Assert.IsFalse(result.IsSuccess);
		Assert.IsNotNull(result.Error);
	}

	[TestMethod]
	public async Task CreateCheckout_EscapesMerchantAndReaderInUrl()
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.OK, CheckoutResponse);

		await host.Client.CreateCheckoutAsync("MH 4/H", "rdr/../x", BuildRequest());

		Assert.AreEqual("https://api.sumup.com/v0.1/merchants/MH%204%2FH/readers/rdr%2F..%2Fx/checkout", host.Handler.Requests.Single().Uri.AbsoluteUri);
	}

	[TestMethod]
	public async Task CreateCheckout_WithoutActiveCredential_FailsWithoutCallingSumUp()
	{
		using var host = await SumUpTestHost.CreateAsync(null);

		var result = await host.Client.CreateCheckoutAsync(SumUpTestHost.MerchantCode, SumUpTestHost.ReaderId, BuildRequest());

		Assert.IsFalse(result.IsSuccess);
		Assert.IsEmpty(host.Handler.Requests);
	}

	[TestMethod]
	public async Task CreateCheckout_UsesNewApiKeyAfterCredentialChange()
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.OK, CheckoutResponse);
		host.Handler.Enqueue(HttpStatusCode.OK, CheckoutResponse);

		await host.Client.CreateCheckoutAsync(SumUpTestHost.MerchantCode, SumUpTestHost.ReaderId, BuildRequest());
		await host.SetActiveCredentialAsync(SumUpTestHost.BuildCredential("sup_sk_other_key"));
		await host.Client.CreateCheckoutAsync(SumUpTestHost.MerchantCode, SumUpTestHost.ReaderId, BuildRequest());

		Assert.AreEqual("Bearer sup_sk_test_key", host.Handler.Requests[0].Authorization);
		Assert.AreEqual("Bearer sup_sk_other_key", host.Handler.Requests[1].Authorization);
	}
}
