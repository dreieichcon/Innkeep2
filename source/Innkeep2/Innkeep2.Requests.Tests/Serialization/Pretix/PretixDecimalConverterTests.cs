using Innkeep2.Requests.Serialization.Pretix;

namespace Innkeep2.Requests.Tests.Serialization.Pretix;

[TestClass]
public class PretixDecimalConverterTests
{
	[DataRow("10.0", 10.0)]
	[DataRow("10,0", 10.0)]
	[DataRow("10,00", 10.0)]
	[DataRow("10.00", 10.0)]
	[DataRow("10", 10.0)]
	[DataRow("10.0€", 10.0)]
	[DataRow("10,0€", 10.0)]
	[DataRow("1.234,56", 1234.56)]
	[DataRow("1,234.56", 1234.56)]
	[DataRow("1234.56", 1234.56)]
	[DataRow("0,5", 0.5)]
	[DataRow("-10,50", -10.50)]
	[TestMethod]
	public void Parse_ReturnsExpectedDecimal(string input, double expected)
	{
		var result = PretixDecimalConverter.Parse(input);
		Assert.AreEqual((decimal)expected, result);
	}
}