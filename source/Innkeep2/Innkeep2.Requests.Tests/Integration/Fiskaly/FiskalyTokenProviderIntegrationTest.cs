using Innkeep2.Requests.Fiskaly.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace Innkeep2.Requests.Tests.Integration.Fiskaly;

[TestClass]
public class FiskalyTokenProviderIntegrationTests : FiskalyIntegrationTestBase
{
    [TestMethod]
    public async Task GetHeaderAsync_ReturnsToken()
    {
        var provider = ServiceProvider.GetRequiredService<FiskalyTokenProvider>();

        var result = await provider.GetHeaderAsync();

        Assert.IsTrue(result.IsSuccess);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Value!.Parameter));
    }

    [TestMethod]
    public async Task GetHeaderAsync_CalledTwice_ReturnsCachedToken()
    {
        var provider = ServiceProvider.GetRequiredService<FiskalyTokenProvider>();

        var first = await provider.GetHeaderAsync();
        var second = await provider.GetHeaderAsync();

        Assert.AreEqual(first.Value!.Parameter, second.Value!.Parameter);
    }
}