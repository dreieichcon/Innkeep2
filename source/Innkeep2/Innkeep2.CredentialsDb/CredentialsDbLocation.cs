namespace Innkeep2.CredentialsDb;

public static class CredentialsDbLocation
{
    private const string Directory = "./db";
    private const string FilePath = "./db/credentials.db";

    /// <summary>
    /// Ensures the database directory exists and returns the credentials database path.
    /// </summary>
    public static string EnsureCreated()
    {
        System.IO.Directory.CreateDirectory(Directory);
        return FilePath;
    }
}
