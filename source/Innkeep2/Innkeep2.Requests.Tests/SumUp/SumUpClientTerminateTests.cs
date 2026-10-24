using System.Net;

namespace Innkeep2.Requests.Tests.SumUp;

[TestClass]
public class SumUpClientTerminateTests
{
	[TestMethod]
	public async Task Terminate_SendsPostToDocumentedTerminateUrl()
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.Accepted);

		await host.Client.TerminateAsync(SumUpTestHost.MerchantCode, SumUpTestHost.ReaderId);

		var request = host.Handler.Requests.Single();
		Assert.AreEqual(HttpMethod.Post, request.Method);
		Assert.AreEqual($"https://api.sumup.com/v0.1/merchants/{SumUpTestHost.MerchantCode}/readers/{SumUpTestHost.ReaderId}/terminate", request.Uri.AbsoluteUri);
	}

	[TestMethod]
	public async Task Terminate_SendsApiKeyAsBearerToken()
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.Accepted);

		await host.Client.TerminateAsync(SumUpTestHost.MerchantCode, SumUpTestHost.ReaderId);

		Assert.AreEqual($"Bearer {SumUpTestHost.ApiKey}", host.Handler.Requests.Single().Authorization);
	}

	[DataRow(HttpStatusCode.OK)]
	[DataRow(HttpStatusCode.Accepted)]
	[DataRow(HttpStatusCode.NoContent)]
	[TestMethod]
	public async Task Terminate_ReturnsSuccessForSuccessStatusWithEmptyBody(HttpStatusCode status)
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(status);

		var result = await host.Client.TerminateAsync(SumUpTestHost.MerchantCode, SumUpTestHost.ReaderId);

		Assert.IsTrue(result.IsSuccess);
	}

	[DataRow(HttpStatusCode.BadRequest)]
	[DataRow(HttpStatusCode.Unauthorized)]
	[DataRow(HttpStatusCode.NotFound)]
	[DataRow(HttpStatusCode.UnprocessableEntity)]
	[DataRow(HttpStatusCode.InternalServerError)]
	[TestMethod]
	public async Task Terminate_ReturnsFailureForErrorStatus(HttpStatusCode status)
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(status, """{"type":"about:blank","title":"Error","status":400,"detail":"Not awaiting cardholder"}""");

		var result = await host.Client.TerminateAsync(SumUpTestHost.MerchantCode, SumUpTestHost.ReaderId);

		Assert.IsFalse(result.IsSuccess);
		Assert.IsNotNull(result.Error);
	}

	[TestMethod]
	public async Task Terminate_WithoutActiveCredential_FailsWithoutCallingSumUp()
	{
		using var host = await SumUpTestHost.CreateAsync(null);

		var result = await host.Client.TerminateAsync(SumUpTestHost.MerchantCode, SumUpTestHost.ReaderId);

		Assert.IsFalse(result.IsSuccess);
		Assert.IsEmpty(host.Handler.Requests);
	}
}
