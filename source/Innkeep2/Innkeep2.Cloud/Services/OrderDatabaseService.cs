using Innkeep2.Cloud.Orders;
using Innkeep2.Models.Core;
using Innkeep2.Services.Cloud;
using Microsoft.EntityFrameworkCore;

namespace Innkeep2.Cloud.Services;

public sealed class OrderDatabaseService(IActiveConfigurationService activeConfiguration)
{
    private const string DatabaseDirectory = "./db/orders";

    public async Task<Result<string>> CreateAndSelectAsync(
        string organizerSlug,
        string eventSlug,
        CancellationToken ct = default
    )
    {
        Directory.CreateDirectory(DatabaseDirectory);

        var fileName = $"{organizerSlug}-{eventSlug}-{DateTime.UtcNow:yyyy-MM-dd}.db";
        var path = Path.Combine(DatabaseDirectory, fileName);

        var options = new DbContextOptionsBuilder<InnkeepOrderDbContext>()
            .UseSqlite($"Data Source={path}")
            .Options;

        await using var context = new InnkeepOrderDbContext(options);
        await context.Database.MigrateAsync(ct);

        await activeConfiguration.SetOrderDatabasePath(path);
        await activeConfiguration.SaveAsync();

        return Result<string>.Success(path);
    }
    
    public IReadOnlyList<string> ListAvailable()
        => Directory.Exists(DatabaseDirectory)
            ? Directory.GetFiles(DatabaseDirectory, "*.db")
            : [];
}