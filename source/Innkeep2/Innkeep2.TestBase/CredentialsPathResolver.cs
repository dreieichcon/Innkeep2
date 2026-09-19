namespace Innkeep2.TestBase;

public static class CredentialsPathResolver
{
    public static string ResolveCredentialsPath()
    {
        var directory = Environment.GetEnvironmentVariable("INNKEEP2_CREDENTIALS_DIR")
                        ?? Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "credentials");

        var path = Path.Combine(directory, "credentials.test.json");

        return File.Exists(path)
            ? path
            : throw new FileNotFoundException(
                $"Credentials file not found at '{path}'. Set INNKEEP2_CREDENTIALS_DIR to override.", path);
    }
}