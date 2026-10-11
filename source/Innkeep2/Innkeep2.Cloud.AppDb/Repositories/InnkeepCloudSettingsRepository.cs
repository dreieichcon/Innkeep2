using Innkeep2.Cloud.AppDb.Models;
using Microsoft.EntityFrameworkCore;

namespace Innkeep2.Cloud.AppDb.Repositories;

public class InnkeepCloudSettingsRepository(IDbContextFactory<InnkeepCloudDbContext> contextFactory)
	: InnkeepCloudRepository<InnkeepCloudSettings>(contextFactory);
