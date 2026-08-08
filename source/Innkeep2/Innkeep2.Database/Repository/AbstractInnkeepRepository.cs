using Innkeep2.Database.Model;
using Innkeep2.Models.Core;
using Microsoft.EntityFrameworkCore;

namespace Innkeep2.Database.Repository;

public abstract class AbstractRepository<TEntity, TContext>(IDbContextFactory<TContext> contextFactory)
	where TEntity : class, IDbItem
	where TContext : DbContext
{
	protected virtual TContext CreateContext() => contextFactory.CreateDbContext();

	protected virtual DbSet<TEntity> GetSet(TContext context) => context.Set<TEntity>();

	public virtual Result<IReadOnlyList<TEntity>> GetAll()
	{
		using var context = CreateContext();

		return Result<IReadOnlyList<TEntity>>.Success(
			GetSet(context)
				.ToList()
		);
	}

	public virtual async Task<Result<IReadOnlyList<TEntity>>> GetAllAsync(CancellationToken ct = default)
	{
		await using var context = CreateContext();

		var entities = await GetSet(context)
			.ToListAsync(ct);

		return Result<IReadOnlyList<TEntity>>.Success(entities);
	}

	public virtual Result<TEntity> Get(int id)
	{
		using var context = CreateContext();

		var entity = GetSet(context)
			.Find(id);

		return ToResult(entity, id);
	}

	public virtual async Task<Result<TEntity>> GetAsync(int id, CancellationToken ct = default)
	{
		await using var context = CreateContext();

		var entity = await GetSet(context)
			.FindAsync([id], ct);

		return ToResult(entity, id);
	}

	public virtual Result<TEntity> Crud(TEntity entity) => entity.Operation switch
	{
		Operation.Create => Create(entity),
		Operation.Update => Update(entity),
		Operation.Delete => Delete(entity),
		Operation.None => Result<TEntity>.Success(entity),
		_ => Result<TEntity>.Failure(new Error("Db.UnknownOperation", $"Unhandled operation '{entity.Operation}'."))
	};

	public virtual Task<Result<TEntity>> CrudAsync(TEntity entity, CancellationToken ct = default)
		=> entity.Operation switch
		{
			Operation.Create => CreateAsync(entity, ct),
			Operation.Update => UpdateAsync(entity, ct),
			Operation.Delete => DeleteAsync(entity, ct),
			Operation.None => Task.FromResult(Result<TEntity>.Success(entity)),
			_ => Task.FromResult(
				Result<TEntity>.Failure(new Error("Db.UnknownOperation", $"Unhandled operation '{entity.Operation}'."))
			)
		};

	public virtual Result<IReadOnlyList<TEntity>> CrudMany(IEnumerable<TEntity> items)
	{
		var entities = items.ToList();
		using var context = CreateContext();
		var set = GetSet(context);

		foreach (var entity in entities)
		{
			switch (entity.Operation)
			{
				case Operation.Create:
					set.Add(entity);
					break;

				case Operation.Update: 
					set.Update(entity); 
					break;

				case Operation.Delete: 
					set.Remove(entity); 
					break;

				case Operation.None: 
					break;

				default:
					return Result<IReadOnlyList<TEntity>>.Failure(
						new Error(
							"Db.UnknownOperation",
							$"Unhandled operation '{entity.Operation}' on entity {entity.Id}."
						)
					);
			}
		}

		try
		{
			context.SaveChanges();
			return Result<IReadOnlyList<TEntity>>.Success(entities);
		}
		catch (DbUpdateException ex)
		{
			return Result<IReadOnlyList<TEntity>>.Failure(new Error("Db.SaveFailed", ex.Message, ex));
		}
	}

	public virtual async Task<Result<IReadOnlyList<TEntity>>> CrudManyAsync(
		IEnumerable<TEntity> items,
		CancellationToken ct = default
	)
	{
		var entities = items.ToList();
		await using var context = CreateContext();
		var set = GetSet(context);

		foreach (var entity in entities)
		{
			switch (entity.Operation)
			{
				case Operation.Create: set.Add(entity); break;

				case Operation.Update: set.Update(entity); break;

				case Operation.Delete: set.Remove(entity); break;

				case Operation.None: break;

				default:
					return Result<IReadOnlyList<TEntity>>.Failure(
						new Error(
							"Db.UnknownOperation",
							$"Unhandled operation '{entity.Operation}' on entity {entity.Id}."
						)
					);
			}
		}

		try
		{
			await context.SaveChangesAsync(ct);
			return Result<IReadOnlyList<TEntity>>.Success(entities);
		}
		catch (DbUpdateException ex)
		{
			return Result<IReadOnlyList<TEntity>>.Failure(new Error("Db.SaveFailed", ex.Message, ex));
		}
	}

	public virtual Result<TEntity> Create(TEntity entity)
	{
		using var context = CreateContext();

		GetSet(context)
			.Add(entity);

		return TrySave(context, entity);
	}

	public virtual async Task<Result<TEntity>> CreateAsync(TEntity entity, CancellationToken ct = default)
	{
		await using var context = CreateContext();

		GetSet(context)
			.Add(entity);

		return await TrySaveAsync(context, entity, ct);
	}

	public virtual Result<TEntity> Update(TEntity entity)
	{
		using var context = CreateContext();

		GetSet(context)
			.Update(entity);

		return TrySave(context, entity);
	}

	public virtual async Task<Result<TEntity>> UpdateAsync(TEntity entity, CancellationToken ct = default)
	{
		await using var context = CreateContext();

		GetSet(context)
			.Update(entity);

		return await TrySaveAsync(context, entity, ct);
	}

	public virtual Result<TEntity> Delete(TEntity entity)
	{
		using var context = CreateContext();

		GetSet(context)
			.Remove(entity);

		return TrySave(context, entity);
	}

	public virtual async Task<Result<TEntity>> DeleteAsync(TEntity entity, CancellationToken ct = default)
	{
		await using var context = CreateContext();

		GetSet(context)
			.Remove(entity);

		return await TrySaveAsync(context, entity, ct);
	}

	private static Result<TEntity> TrySave(TContext context, TEntity entity)
	{
		try
		{
			context.SaveChanges();
			return Result<TEntity>.Success(entity);
		}
		catch (DbUpdateException ex)
		{
			return Result<TEntity>.Failure(new Error("Db.SaveFailed", ex.Message, ex));
		}
	}

	private static async Task<Result<TEntity>> TrySaveAsync(TContext context, TEntity entity, CancellationToken ct)
	{
		try
		{
			await context.SaveChangesAsync(ct);
			return Result<TEntity>.Success(entity);
		}
		catch (DbUpdateException ex)
		{
			return Result<TEntity>.Failure(new Error("Db.SaveFailed", ex.Message, ex));
		}
	}

	private static Result<TEntity> ToResult(TEntity? entity, int id) => entity is not null
		? Result<TEntity>.Success(entity)
		: Result<TEntity>.Failure(new Error("Db.NotFound", $"{typeof(TEntity).Name} with id '{id}' not found."));
}