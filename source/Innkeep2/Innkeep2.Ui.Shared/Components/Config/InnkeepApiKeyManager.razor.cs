using Innkeep2.CredentialsDb.Models;
using Microsoft.AspNetCore.Components;

namespace Innkeep2.Ui.Shared.Components.Config;

public partial class InnkeepApiKeyManager
{
    [Parameter]
    public string Title { get; set; } = "API Key";

    private static InnkeepApiKey CreateNew() => new()
    {
        Name = string.Empty,
        ApiKey = string.Empty,
        Url = string.Empty
    };

    private static InnkeepApiKey Clone(InnkeepApiKey source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        Active = source.Active,
        ApiKey = source.ApiKey,
        Url = source.Url
    };
}
