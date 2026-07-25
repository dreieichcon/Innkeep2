namespace Innkeep2.Models.Core;

public sealed record Error(
	string Code,
	string Message,
	Exception? Exception = null,
	IReadOnlyDictionary<string, object?>? Metadata = null);