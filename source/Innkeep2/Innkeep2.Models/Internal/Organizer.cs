using Innkeep2.Models.Pretix;

namespace Innkeep2.Models.Internal;

public sealed record Organizer
{
	public required string Name { get; set; }
	
	public required string Slug { get; set; }
	
	public static Organizer FromPretix(PretixOrganizer organizer)
		=> new()
		{
			Name = organizer.Name,
			Slug = organizer.Slug
		};
}