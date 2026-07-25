namespace Innkeep2.Credentials.Models;

public sealed record PretixCredential(string Name, string ApiKey, string BaseUrl, bool Active, bool IsTest = false)
	: ICredential;