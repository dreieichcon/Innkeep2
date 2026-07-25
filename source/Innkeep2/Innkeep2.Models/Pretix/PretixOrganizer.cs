using System.Text.Json.Serialization;

namespace Innkeep2.Models.Pretix;

public record PretixOrganizer
{
	[JsonPropertyName("name")]
	public required string Name { get; init; }
	
	[JsonPropertyName("slug")]
	public required string Slug { get; init; }
	
	[JsonPropertyName("public_url")]
	public required string PublicUrl { get; init; }

	[JsonPropertyName("plugins")]
	public IList<string> Plugins { get; init; } = [];
}