using System.Security.Claims;
using RoomMitra.Application.Abstractions.Security;

namespace RoomMitra.Api.Security;

public sealed class HttpUserContext : IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user is null || user.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(id, out var parsed) ? parsed : null;
        }
    }
}
