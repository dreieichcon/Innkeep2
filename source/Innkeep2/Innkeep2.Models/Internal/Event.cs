using Innkeep2.Models.Pretix;

namespace Innkeep2.Models.Internal;

public sealed record Event
{
	public required string Name { get; set; }
	
	public required string Slug { get; set; }
	
	public required bool IsTestMode { get; set; }
	
	public static Event FromPretix(PretixEvent pEvent)
		=> new()
		{
			Name = pEvent.Name.German,
			Slug = pEvent.Slug,
			IsTestMode = pEvent.TestMode
		};
}