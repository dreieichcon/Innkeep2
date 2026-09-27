namespace Innkeep2.Models.Core;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount);