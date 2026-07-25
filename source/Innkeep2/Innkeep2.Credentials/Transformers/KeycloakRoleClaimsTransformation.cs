using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;

namespace Innkeep2.Credentials.Transformers;

public class KeycloakRoleClaimsTransformation(IConfiguration configuration) : IClaimsTransformation
{
	public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
	{
		var identity = (ClaimsIdentity)principal.Identity!;
		var clientId = configuration["keycloak:clientid"]!;

		if (string.IsNullOrEmpty(clientId))
			throw new InvalidOperationException("Oidc:ClientId is not configured — check credentials.json.");

		var resourceAccessJson = identity.FindFirst("resource_access")?.Value;
		if (resourceAccessJson is null)
			return Task.FromResult(principal);

		using var doc = JsonDocument.Parse(resourceAccessJson);
		if (!doc.RootElement.TryGetProperty(clientId, out var clientElement) ||
			!clientElement.TryGetProperty("roles", out var rolesElement))
			return Task.FromResult(principal);

		foreach (var role in rolesElement.EnumerateArray())
		{
			var roleName = role.GetString();
			if (roleName is not null && !identity.HasClaim(ClaimTypes.Role, roleName))
				identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
		}

		return Task.FromResult(principal);
	}
}