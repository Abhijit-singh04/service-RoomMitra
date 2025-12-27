using System.Security.Claims;

namespace RoomMitra.Application.Abstractions.Auth;

public interface ITokenService
{
    string CreateAccessToken(IEnumerable<Claim> claims);
}
