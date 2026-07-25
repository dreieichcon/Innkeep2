using Innkeep2.Credentials.Models;
using Innkeep2.Credentials.Options;
using Microsoft.Extensions.Options;

namespace Innkeep2.Credentials;

public sealed class ActiveCredentialsProvider<T>(IOptionsMonitor<CredentialsOptions<T>> options) where T : ICredential
{
	public T GetActive() =>
		options.CurrentValue.Keys.FirstOrDefault(k => k.Active)
		?? throw new InvalidOperationException($"No active {typeof(T).Name} configured.");
}