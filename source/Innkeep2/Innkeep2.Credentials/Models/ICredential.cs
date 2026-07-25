namespace Innkeep2.Credentials.Models;

public interface ICredential
{
	string Name { get; }
	bool Active { get; }
	bool IsTest { get; }
}