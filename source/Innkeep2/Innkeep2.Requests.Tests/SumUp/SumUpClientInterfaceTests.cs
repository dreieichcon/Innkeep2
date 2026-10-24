using System.Net;
using Innkeep2.Models.SumUp;
using Innkeep2.Requests.SumUp;
using Microsoft.Extensions.DependencyInjection;

namespace Innkeep2.Requests.Tests.SumUp;

[TestClass]
public class SumUpClientInterfaceTests
{
	[TestMethod]
	public async Task ISumUpClient_ResolvesToTheConfiguredClient()
	{
		using var host = await SumUpTestHost.CreateWithCredentialAsync();
		host.Handler.Enqueue(HttpStatusCode.OK, """{"data":{"client_transaction_id":"abc"}}""");

		var result = await host.Provider.GetRequiredService<ISumUpClient>().CreateCheckoutAsync(
			SumUpTestHost.MerchantCode,
			SumUpTestHost.ReaderId,
			new SumUpCheckoutRequest { TotalAmount = SumUpMoney.FromDecimal(1m, "EUR") });

		Assert.IsTrue(result.IsSuccess);
		Assert.AreEqual($"Bearer {SumUpTestHost.ApiKey}", host.Handler.Requests.Single().Authorization);
	}
}
