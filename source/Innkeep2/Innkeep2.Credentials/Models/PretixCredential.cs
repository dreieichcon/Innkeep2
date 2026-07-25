using JetBrains.Annotations;

namespace Innkeep2.Credentials.Models;

[UsedImplicitly]
public sealed record PretixCredential(string Name, string ApiKey, string BaseUrl, bool Active, bool IsTest = false)
	: ICredential;