using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Innkeep2.Models.Core;

[UsedImplicitly]
public sealed record Error(
	string Code,
	string Message,
	[property: JsonIgnore] Exception? Exception = null,
	IReadOnlyDictionary<string, object?>? Metadata = null);