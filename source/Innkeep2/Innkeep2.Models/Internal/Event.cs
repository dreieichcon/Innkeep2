using System.Text;
using Innkeep2.Models.Pretix;

namespace Innkeep2.Models.Internal;

public sealed record Event
{
	private const string FallbackLocale = "de";

	public required string Name { get; set; }
	
	public required string Slug { get; set; }
	
	public required bool IsTestMode { get; set; }
	
	public string? Header { get; set; }

	public string OrderLocale { get; set; } = FallbackLocale;

	public static Event FromPretix(PretixEvent pEvent)
		=> new()
		{
			Name = pEvent.Name.German,
			Slug = pEvent.Slug,
			IsTestMode = pEvent.TestMode
		};
	
	/// <summary>
	/// Picks the default locale of the event if it is enabled, otherwise the first enabled locale.
	/// Falls back to German when the event has no enabled locales.
	/// </summary>
	public static string ResolveOrderLocale(PretixEventSettings settings)
	{
		var isDefaultEnabled = settings.Locales.Contains(settings.Locale, StringComparer.OrdinalIgnoreCase);

		if (isDefaultEnabled)
			return settings.Locale;

		return settings.Locales.FirstOrDefault() ?? FallbackLocale;
	}

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