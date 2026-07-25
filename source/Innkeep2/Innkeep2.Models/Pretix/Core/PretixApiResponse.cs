using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Innkeep2.Models.Pretix.Core;

[UsedImplicitly]
public sealed record PretixPagedResult<T>
{
	public required int Count { get; init; }

	[JsonPropertyName("next")]
	public string? NextPageUrl { get; init; }

	[JsonPropertyName("previous")]
	public string? PreviousPageUrl { get; init; }

	[UsedImplicitly]
	public required IReadOnlyList<T> Results { get; init; }
}