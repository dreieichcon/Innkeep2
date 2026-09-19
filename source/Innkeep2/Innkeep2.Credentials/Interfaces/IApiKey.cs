namespace Innkeep2.Credentials.Interfaces;

public interface IApiKey
{
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool Revoked { get; set; }
}