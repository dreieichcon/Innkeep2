using Innkeep2.Ui.Shared.Layout;
using MudBlazor;

namespace Innkeep2.Server.Components.Layout;

public static class NavigationSections
{
	public static IReadOnlyList<NavSection> All { get; } =
	[
		new("Verkauf", Icons.Material.Outlined.AttachMoney,
		[
			new("Produkte", "/SalesItems"),
			new("Transaktionen", "/Transactions/Overview"),
			new("Geld Buchen", "/Transactions/Transfer")
		]),
		new("Betrieb", Icons.Material.Outlined.Business,
		[
			new("Queue", "/Queue"),
			new("Drucken", "/Print")
		]),
		new("Einstellungen", Icons.Material.Outlined.Settings,
		[
			new("Zugangsdaten", "/Config/Credentials"),
			new("API-Schlüssel", "/Config/Api"),
			new("Drucker", "/Config/Printer")
		])
	];
}
