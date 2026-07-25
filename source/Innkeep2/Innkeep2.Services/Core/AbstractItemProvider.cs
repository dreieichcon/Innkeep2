using Innkeep2.Models.Core;
using Innkeep2.Models.Pretix.Core;
using Microsoft.Extensions.Caching.Memory;

namespace Inkeep2.Services.Core;

public abstract class AbstractItemProvider<T, TInternal>(IMemoryCache cache)
{
	protected abstract TimeSpan CacheDuration { get; }

	protected abstract string Key { get; }

	protected abstract Task<Result<PretixPagedResult<T>>> FetchAsync(CancellationToken ct);

	protected abstract TInternal Map(T item);

	public async Task<Result<IReadOnlyList<TInternal>>> GetCachedItemsAsync(CancellationToken ct = default)
	{
		if (cache.TryGetValue(Key, out IReadOnlyList<TInternal>? cached))
			return Result<IReadOnlyList<TInternal>>.Success(cached!);

		var result = await FetchAsync(ct);

		if (!result.IsSuccess)
			return Result<IReadOnlyList<TInternal>>.Failure(result.Error!);

		var items = result.Value!.Results.Select(Map)
			.ToList();

		cache.Set(Key, items, CacheDuration);

		return Result<IReadOnlyList<TInternal>>.Success(items);
	}
}