using Innkeep2.Services.Cloud;

namespace Innkeep2.Cloud.Services;

public sealed class StatusBarService(IActiveConfigurationService activeConfiguration)
{
	public string? OrganizerName => activeConfiguration.Organizer?.Name;

	public string? EventName => activeConfiguration.Event?.Name;

	public string? TssName => activeConfiguration.Tss?.SerialNumber;

	public event EventHandler? Changed
	{
		add => activeConfiguration.Changed += value;
		remove => activeConfiguration.Changed -= value;
	}
}