using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RoomMitra.Application.Abstractions.Auth;
using RoomMitra.Infrastructure.Options;

namespace RoomMitra.Infrastructure.Auth;

internal sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public string CreateAccessToken(IEnumerable<Claim> claims)
    {
        if (string.IsNullOrWhiteSpace(_options.SigningKey))
        {
            throw new InvalidOperationException("JWT signing key is not configured.");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey))
        {
            KeyId = string.Empty // Explicitly set empty KeyId to avoid kid in token header
        };
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes),
            signingCredentials: credentials
        );

        var handler = new JwtSecurityTokenHandler();
        handler.SetDefaultTimesOnTokenCreation = false; // Don't add default timestamps
        return handler.WriteToken(token);
    }
}
