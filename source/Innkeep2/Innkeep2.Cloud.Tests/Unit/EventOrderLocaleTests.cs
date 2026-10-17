using Innkeep2.Models.Internal;
using Innkeep2.Models.Pretix;

namespace Innkeep2.Cloud.Tests.Unit;

[TestClass]
public class EventOrderLocaleTests
{
    [TestMethod]
    public void ResolveOrderLocale_DefaultLocaleIsEnabled_ReturnsDefaultLocale()
    {
        var settings = new PretixEventSettings { Locale = "de-informal", Locales = ["en", "de-informal"] };

        var locale = Event.ResolveOrderLocale(settings);

        Assert.AreEqual("de-informal", locale);
    }

    [TestMethod]
    public void ResolveOrderLocale_DefaultLocaleIsNotEnabled_ReturnsFirstEnabledLocale()
    {
        var settings = new PretixEventSettings { Locale = "fr", Locales = ["en", "de-informal"] };

        var locale = Event.ResolveOrderLocale(settings);

        Assert.AreEqual("en", locale);
    }

    [TestMethod]
    public void ResolveOrderLocale_NoEnabledLocales_ReturnsGerman()
    {
        var settings = new PretixEventSettings { Locale = "en", Locales = [] };

        var locale = Event.ResolveOrderLocale(settings);

        Assert.AreEqual("de", locale);
    }

    [TestMethod]
    public void OrderLocale_NotResolved_DefaultsToGerman()
    {
        var pretixEvent = new Event { Name = "", Slug = "", IsTestMode = false };

        Assert.AreEqual("de", pretixEvent.OrderLocale);
    }
}
