using Demolite.Db.Interfaces;
using Demolite.Db.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Innkeep2.Cloud.AppDb.Repositories;

public abstract class InnkeepCloudRepository<TEntity>(IDbContextFactory<InnkeepCloudDbContext> contextFactory)
	: AbstractRepository<TEntity, int, InnkeepCloudDbContext>(contextFactory)
	where TEntity : class, IDbItem<int>;
