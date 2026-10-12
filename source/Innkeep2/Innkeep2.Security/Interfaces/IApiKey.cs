namespace Innkeep2.Security.Interfaces;

public interface IApiKey
{
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool Revoked { get; set; }
}