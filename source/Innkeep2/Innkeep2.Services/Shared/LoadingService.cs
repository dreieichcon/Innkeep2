namespace Innkeep2.Services.Shared;

public class LoadingService
{
    public event EventHandler? LoadingChanged;
    
    public bool IsLoading
    {
        get;
        set
        {
            field = value;
            LoadingChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}