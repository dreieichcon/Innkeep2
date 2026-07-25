using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Innkeep2.Models.Pretix;

[UsedImplicitly]
public record PretixOrganizer
{
	[JsonPropertyName("name")]
	public required string Name { get; init; }
	
	[UsedImplicitly]
	[JsonPropertyName("slug")]
	public required string Slug { get; init; }
	
	[JsonPropertyName("public_url")]
	public required string PublicUrl { get; init; }

	[JsonPropertyName("plugins")]
	[UsedImplicitly]
	public IList<string> Plugins { get; init; } = [];
}