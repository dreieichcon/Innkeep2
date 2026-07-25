using JetBrains.Annotations;

namespace Innkeep2.Models.Core;

[UsedImplicitly]
public sealed record Error(
	string Code,
	string Message,
	Exception? Exception = null,
	IReadOnlyDictionary<string, object?>? Metadata = null);