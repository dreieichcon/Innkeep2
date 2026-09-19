using System.Text;
using Innkeep2.Models.Pretix;

namespace Innkeep2.Models.Internal;

public sealed record Event
{
	public required string Name { get; set; }
	
	public required string Slug { get; set; }
	
	public required bool IsTestMode { get; set; }
	
	public string? Header { get; set; }
	
	public static Event FromPretix(PretixEvent pEvent)
		=> new()
		{
			Name = pEvent.Name.German,
			Slug = pEvent.Slug,
			IsTestMode = pEvent.TestMode
		};
	
	public static string BuildHeader(PretixEventSettings settings)
	{
		var sb = new StringBuilder();

		sb.AppendLine(settings.InvoiceCompanyName);
		sb.AppendLine(settings.InvoiceStreetAddress);
		sb.AppendLine($"{settings.InvoiceZipCode} {settings.InvoiceCity}");
		sb.AppendLine(settings.InvoiceCountry);

		if (!string.IsNullOrEmpty(settings.InvoiceVatId))
			sb.AppendLine($"Ust.Id: {settings.InvoiceVatId}");

		return sb.ToString();
	}
}