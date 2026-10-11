using Innkeep2.Cloud.AppDb.Models;
using Microsoft.EntityFrameworkCore;

namespace Innkeep2.Cloud.AppDb.Repositories;

public class InnkeepCloudApiKeyRepository(IDbContextFactory<InnkeepCloudDbContext> contextFactory)
    : InnkeepCloudRepository<InnkeepCloudApiKey>(contextFactory);
