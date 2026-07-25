using JetBrains.Annotations;

namespace Innkeep2.Credentials.Models;

[UsedImplicitly]
public sealed record FiskalyCredential(string Name, string ApiKey, string ApiSecret, bool Active, bool IsTest = false)
	: ICredential;