namespace Innkeep2.Credentials;

public static class CredentialCreator
{
    public static string ServerJsonTemplate = """
                                              {
                                                "Cloud": {
                                                  "ApiKey": "",
                                                  "CloudUrl": ""
                                                }
                                              }
                                              """;
    
    public static bool EnsureExists(string path, string templateJson)
    {
        if (File.Exists(path))
            return false;

        var directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(path, templateJson);

        return true;
    }
}