using System.Security.Claims;
using RoomMitra.Application.Abstractions.Security;

namespace RoomMitra.Api.Security;

/// <summary>
/// HTTP context-based implementation of IUserContext.
/// Extracts user identity information from Azure AD B2C JWT claims.
/// </summary>
public sealed class HttpUserContext : IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    // Azure AD B2C claim types
    private const string ObjectIdClaimType = "oid";
    private const string IdentityProviderClaimType = "idp";
    private const string EmailsClaimType = "emails";
    private const string NameClaimType = "name";
    private const string GivenNameClaimType = "given_name";
    private const string FamilyNameClaimType = "family_name";

    public HttpUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public Guid? UserId
    {
        get
        {
            if (!IsAuthenticated) return null;

            // Try standard NameIdentifier claim first (used by local JWT auth)
            var nameIdentifier = User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(nameIdentifier) && Guid.TryParse(nameIdentifier, out var nameIdGuid))
                return nameIdGuid;

            // Try Object ID (preferred for Azure AD B2C)
            var oid = ObjectId;
            if (!string.IsNullOrEmpty(oid) && Guid.TryParse(oid, out var oidGuid))
                return oidGuid;

            // Fall back to subject claim
            var sub = Subject;
            if (!string.IsNullOrEmpty(sub) && Guid.TryParse(sub, out var subGuid))
                return subGuid;

            return null;
        }
    }

    public string? ObjectId => User?.FindFirstValue(ObjectIdClaimType);

    public string? Subject => User?.FindFirstValue(ClaimTypes.NameIdentifier)
                            ?? User?.FindFirstValue("sub");

    public string? Email
    {
        get
        {
            // Azure AD B2C uses "emails" claim (array)
            var emails = User?.FindFirstValue(EmailsClaimType);
            if (!string.IsNullOrEmpty(emails))
                return emails;

            // Fall back to standard email claim
            return User?.FindFirstValue(ClaimTypes.Email)
                ?? User?.FindFirstValue("email");
        }
    }

    public string? Name
    {
        get
        {
            // Try the name claim first
            var name = User?.FindFirstValue(NameClaimType);
            if (!string.IsNullOrEmpty(name))
                return name;

            // Construct from given and family name
            var givenName = User?.FindFirstValue(GivenNameClaimType);
            var familyName = User?.FindFirstValue(FamilyNameClaimType);

            if (!string.IsNullOrEmpty(givenName) || !string.IsNullOrEmpty(familyName))
                return $"{givenName} {familyName}".Trim();

            // Fall back to standard name claim
            return User?.FindFirstValue(ClaimTypes.Name);
        }
    }

    public string? IdentityProvider => User?.FindFirstValue(IdentityProviderClaimType)
                                     ?? User?.FindFirstValue("idp");

    public string? PhoneNumber => User?.FindFirstValue("phone")
                                ?? User?.FindFirstValue(ClaimTypes.MobilePhone)
                                ?? User?.FindFirstValue("phone_number");

    public IReadOnlyList<string> Roles
    {
        get
        {
            if (User is null) return Array.Empty<string>();

            return User.FindAll(ClaimTypes.Role)
                .Select(c => c.Value)
                .Concat(User.FindAll("roles").Select(c => c.Value))
                .Distinct()
                .ToList()
                .AsReadOnly();
        }
    }

    public bool HasRole(string role)
    {
        return Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }
}
