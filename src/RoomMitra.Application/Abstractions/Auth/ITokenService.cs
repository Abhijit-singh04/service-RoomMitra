using System.Security.Claims;

namespace RoomMitra.Application.Abstractions.Auth;

public interface ITokenService
{
    string CreateAccessToken(IEnumerable<Claim> claims);
    
    /// <summary>
    /// Validate a JWT token and extract the claims principal.
    /// Returns null if the token is invalid or expired.
    /// </summary>
    ClaimsPrincipal? ValidateToken(string token);
}
