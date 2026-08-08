using Innkeep2.Cloud.Database.Models;
using Innkeep2.Database.Repository;
using Microsoft.EntityFrameworkCore;

namespace Innkeep2.Cloud.Database.Repositories;

public class InnkeepCloudSettingsRepository(IDbContextFactory<InnkeepCloudDbContext> contextFactory)
	: AbstractRepository<InnkeepCloudSettings, InnkeepCloudDbContext>(contextFactory);