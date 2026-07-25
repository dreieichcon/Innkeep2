using System.Text.Json.Serialization;

namespace Innkeep2.Models.Pretix.Core;

public sealed record PretixPagedResult<T>
{
	public required int Count { get; init; }

	[JsonPropertyName("next")]
	public string? NextPageUrl { get; init; }

	[JsonPropertyName("previous")]
	public string? PreviousPageUrl { get; init; }

	public required IReadOnlyList<T> Results { get; init; }
}