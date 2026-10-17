namespace Innkeep2.Ui.Shared.Layout;

public sealed record NavSection(string Title, string Icon, IReadOnlyList<NavEntry> Entries);

public sealed record NavEntry(string Title, string Href);
