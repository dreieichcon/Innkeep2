using LiteDB;

namespace Innkeep2.Print.Storage;

public sealed class PrinterSettingsRepository(string databasePath)
{
    public PrinterSettings GetOrCreate()
    {
        using var db = new LiteDatabase(databasePath);
        var collection = db.GetCollection<PrinterSettings>("printer");

        var settings = collection.FindById(1);

        if (settings is not null)
            return settings;

        settings = new PrinterSettings { IpAddress = "", Port = 9100 };
        collection.Insert(settings);

        return settings;
    }

    public void Save(PrinterSettings settings)
    {
        using var db = new LiteDatabase(databasePath);
        db.GetCollection<PrinterSettings>("printer").Upsert(settings);
    }
}