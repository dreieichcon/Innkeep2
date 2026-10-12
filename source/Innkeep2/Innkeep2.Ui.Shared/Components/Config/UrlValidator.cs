namespace Innkeep2.Ui.Shared.Components.Config;

public static class UrlValidator
{
    public static readonly Func<string?, string?> Validation = value =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https"
            ? null
            : "Bitte eine gültige URL angeben";
}
