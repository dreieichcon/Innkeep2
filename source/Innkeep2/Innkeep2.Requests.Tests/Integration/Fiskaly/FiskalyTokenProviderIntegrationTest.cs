using Innkeep2.Requests.Fiskaly.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace Innkeep2.Requests.Tests.Integration.Fiskaly;

[TestClass]
public class FiskalyTokenProviderIntegrationTests : FiskalyIntegrationTestBase
{
    [TestMethod]
    public async Task GetTokenAsync_ReturnsToken()
    {
        var provider = ServiceProvider.GetRequiredService<FiskalyTokenProvider>();

        var result = await provider.GetTokenAsync();

        Assert.IsTrue(result.IsSuccess);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Value));
    }

    [TestMethod]
    public async Task GetTokenAsync_CalledTwice_ReturnsCachedToken()
    {
        var provider = ServiceProvider.GetRequiredService<FiskalyTokenProvider>();

        var first = await provider.GetTokenAsync();
        var second = await provider.GetTokenAsync();

        Assert.AreEqual(first.Value, second.Value);
    }
}