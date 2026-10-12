namespace Innkeep2.TestBase;

public static class DevCredentialsDb
{
    private const string SolutionFileName = "Innkeep2.sln";

    /// <summary>
    /// Copies the dev credentials database of a project to a temporary file and returns its path.
    /// </summary>
    public static string CopyToTemp(string projectName)
    {
        var sourcePath = ResolveSourcePath(projectName);
        var targetPath = Path.Combine(Path.GetTempPath(), $"innkeep2-test-{Guid.NewGuid():N}.db");

        using var input = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var output = File.Create(targetPath);
        input.CopyTo(output);

        return targetPath;
    }

    private static string ResolveSourcePath(string projectName)
    {
        var path = Path.Combine(FindSolutionDirectory(), projectName, "db", "credentials.db");

        return File.Exists(path)
            ? path
            : throw new FileNotFoundException($"Dev credentials database not found at '{path}'.", path);
    }

    private static string FindSolutionDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
            directory = directory.Parent;

        return directory?.FullName
               ?? throw new DirectoryNotFoundException($"'{SolutionFileName}' not found above '{AppContext.BaseDirectory}'.");
    }
}
