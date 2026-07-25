using Innkeep2.Credentials.Models;

namespace Innkeep2.Credentials.Options;

public sealed class CredentialsOptions<T> where T : ICredential
{
	public List<T> Keys { get; init; } = [];
}