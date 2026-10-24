using Innkeep2.Models.SumUp;

namespace Innkeep2.Requests.Tests.SumUp;

[TestClass]
public class SumUpMoneyTests
{
	[DataRow("15.00", 1500)]
	[DataRow("50.33", 5033)]
	[DataRow("1.00", 100)]
	[DataRow("0.01", 1)]
	[DataRow("0", 0)]
	[DataRow("10.10", 1010)]
	[DataRow("19.999", 2000)]
	[DataRow("1.005", 101)]
	[DataRow("2.675", 268)]
	[DataRow("1234567.89", 123456789)]
	[TestMethod]
	public void FromDecimal_ReturnsAmountInMinorUnits(string amount, long expectedValue)
	{
		var money = SumUpMoney.FromDecimal(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), "EUR");

		Assert.AreEqual(expectedValue, money.Value);
	}

	[TestMethod]
	public void FromDecimal_UsesTwoMinorUnitDigitsAndGivenCurrency()
	{
		var money = SumUpMoney.FromDecimal(5m, "EUR");

		Assert.AreEqual(2, money.MinorUnit);
		Assert.AreEqual("EUR", money.Currency);
	}
}
