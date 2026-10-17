using Innkeep2.Ui.Shared.Layout;
using MudBlazor;

namespace Innkeep2.Cloud.Components.Layout;

public static class NavigationSections
{
	public static IReadOnlyList<NavSection> All { get; } =
	[
		new("Bestellungen", Icons.Material.Outlined.AttachMoney,
		[
			new("Dashboard", "/Transactions/Dashboard"),
			new("Übersicht", "/Transactions/Overview")
		]),
		new("Veranstaltung", Icons.Material.Outlined.Business,
		[
			new("Produkte", "/SalesItems")
		]),
		new("Einrichtung", Icons.Material.Outlined.Settings,
		[
			new("Pretix", "/Config/Pretix"),
			new("Fiskaly", "/Config/Fiskaly"),
			new("Datenbank", "/Config/Db"),
			new("Zugangsdaten", "/Config/Credentials"),
			new("API-Schlüssel", "/Config/Api")
		]),
		new("Kasse (Fiskaly)", Icons.Material.Outlined.Security,
		[
			new("TSS erzeugen", "/Config/Tss/Create"),
			new("TSS bearbeiten", "/Config/Tss/Edit"),
			new("Clients", "/Config/Client")
		])
	];
}
