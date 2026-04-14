using System.Security.Claims;
using ACore.Abstractions;

namespace AGame.Core.Identity;

/// <summary>
/// Service for create and validate JWT
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Create new JWT with specified <paramref name="claims"/> and expiration time
    /// </summary>
    string Generate(IEnumerable<Claim> claims, out DateTime expires);

    /// <summary>
    /// Validate JWT and return principal with claims, that contains in jwt
    /// </summary>
    ClaimsPrincipal GetPrincipal(string jwt);
}

public static class JwtServiceExtensions
{
    /// <summary>
    /// Store default value in claims
    /// </summary>
    public static Claim[] GetClaimsByEntity((Guid EntityId, string Type, string GrandType) values)
    {
        if (values.EntityId == Guid.Empty || string.IsNullOrEmpty(values.Type) ||
            string.IsNullOrEmpty(values.GrandType))
            throw new ArgumentNullException(nameof(values));
        
        return new[]
        {
            new Claim(ClaimTypes.EntityId, values.EntityId.ToString()),
            new Claim(ClaimTypes.EntityType, values.Type),
            new Claim(ClaimTypes.GrandType, values.GrandType)
        };
    }

    /// <summary>
    /// Get default values from JWT
    /// </summary>
    public static (Guid EntityId, string Type, string GrandType) GetEntityFromJwt(this IJwtService jwtService,
        string jwt)
    {
        var principal = jwtService.GetPrincipal(jwt);

        var entityIdString = principal.Claims.FirstOrDefault(x => x.Type == ClaimTypes.EntityId)?.Value;
        var entityType = principal.Claims.FirstOrDefault(x => x.Type == ClaimTypes.EntityType)?.Value;
        var grandType = principal.Claims.FirstOrDefault(x => x.Type == ClaimTypes.GrandType)?.Value;
        if (string.IsNullOrEmpty(entityIdString) || !Guid.TryParse(entityIdString, out var entityId) ||
            string.IsNullOrEmpty(entityType) || string.IsNullOrEmpty(grandType))
            throw new CellException("Invalid claims in JWT") {Data =
            {
                {"Token", jwt},
                {"EntityId", entityIdString},
                {"EntityType", entityType},
                {"GrandType", grandType}
            }};

        return (entityId, entityType, grandType);
    }
}