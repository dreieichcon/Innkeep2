using Innkeep2.Cloud.AppDb.Models;
using Innkeep2.Database.Repository;
using Microsoft.EntityFrameworkCore;

namespace Innkeep2.Cloud.AppDb.Repositories;

public class InnkeepCloudApiKeyRepository(IDbContextFactory<InnkeepCloudDbContext> contextFactory)
    : AbstractRepository<InnkeepCloudApiKey, InnkeepCloudDbContext>(contextFactory);