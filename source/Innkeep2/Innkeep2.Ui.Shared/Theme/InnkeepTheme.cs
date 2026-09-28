using MudBlazor;

namespace Innkeep2.Ui.Shared.Theme;

public class InnkeepTheme : MudTheme
{
    static InnkeepTheme()
    {
        Theme = new MudTheme();
    }
    
    public static MudTheme Theme { get; set; }
}