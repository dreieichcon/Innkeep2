using System.Text.Json;

namespace Innkeep2.TestBase;

public sealed record TestData(
    string PretixOrganizerSlug,
    string PretixEventSlug,
    Guid FiskalyTssId,
    Guid FiskalyClientId)
{
    private const string FileName = "testdata.json";

    private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Loads the test values from the testdata file next to the test assembly.
    /// </summary>
    public static TestData Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, FileName);

        if (!File.Exists(path))
            throw new FileNotFoundException($"Test data file not found at '{path}'.", path);

        return JsonSerializer.Deserialize<TestData>(File.ReadAllText(path), SerializerOptions)
               ?? throw new InvalidDataException($"Test data file '{path}' is empty.");
    }
}
