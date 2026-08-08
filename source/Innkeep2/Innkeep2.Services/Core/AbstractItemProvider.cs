using Innkeep2.Models.Core;
using Innkeep2.Models.Pretix.Core;
using Microsoft.Extensions.Caching.Memory;

namespace Innkeep2.Services.Core;

public abstract class AbstractItemProvider<TContext, T, TInternal>(IMemoryCache cache)
{
	protected abstract TimeSpan CacheDuration { get; }
	protected abstract string BuildKey(TContext context);
	protected abstract Task<Result<PretixPagedResult<T>>> FetchAsync(TContext context, CancellationToken ct);
	protected abstract IEnumerable<TInternal> Map(T item);

	protected virtual bool FilterFunc(T item) => true;
	
	public async Task<Result<IReadOnlyList<TInternal>>> GetCachedItemsAsync(TContext context, CancellationToken ct = default)
	{
		var key = BuildKey(context);

		if (cache.TryGetValue(key, out IReadOnlyList<TInternal>? cached))
			return Result<IReadOnlyList<TInternal>>.Success(cached!);

		var result = await FetchAsync(context, ct);

		if (!result.IsSuccess)
			return Result<IReadOnlyList<TInternal>>.Failure(result.Error!);

		var items = result.Value!.Results.Where(FilterFunc).SelectMany(Map).ToList();
		cache.Set(key, items, CacheDuration);

		return Result<IReadOnlyList<TInternal>>.Success(items);
	}
}