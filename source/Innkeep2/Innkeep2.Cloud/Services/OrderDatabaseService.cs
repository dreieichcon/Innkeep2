using Innkeep2.Cloud.Orders;
using Innkeep2.Models.Core;
using Innkeep2.Requests.Core;
using Innkeep2.Services.Cloud;
using Microsoft.EntityFrameworkCore;

namespace Innkeep2.Cloud.Services;

public sealed class OrderDatabaseService(IActiveConfigurationService activeConfiguration)
{
    private const string DatabaseDirectory = "./db/orders";

    public async Task<Result<string>> CreateAndSelectAsync(CancellationToken ct = default)
    {
        if (activeConfiguration.Organizer is not { } organizer || activeConfiguration.Event is not { } pretixEvent)
            return Result<string>.Failure(
                new Error("Order.NoConfiguration", "No organizer or event is currently selected."));
        
        Directory.CreateDirectory(DatabaseDirectory);
        
        var fileName = $"{activeConfiguration.Organizer.Slug}-{activeConfiguration.Event.Slug}-{DateTime.UtcNow:yyyy-MM-dd}.db";
        var path = Path.Combine(DatabaseDirectory, fileName);
        
        if (File.Exists(path))
            return Result<string>.Failure(new Error("Order.Database", "Die Datei existiert bereits."));

        var options = new DbContextOptionsBuilder<InnkeepOrderDbContext>()
            .UseSqlite($"Data Source={path}")
            .Options;

        await using var context = new InnkeepOrderDbContext(options);
        await context.Database.MigrateAsync(ct);

        await activeConfiguration.SetOrderDatabasePath(path);
        await activeConfiguration.SaveAsync(ct);

        return Result<string>.Success(path);
    }

    public async Task<Result<Unit>> SetAsync(string? path, CancellationToken ct = default)
    {
        await activeConfiguration.SetOrderDatabasePath(path);
        return await activeConfiguration.SaveAsync(ct);
    }
    
    public IReadOnlyList<string> ListAvailable()
        => Directory.Exists(DatabaseDirectory)
            ? Directory.GetFiles(DatabaseDirectory, "*.db")
            : [];
}