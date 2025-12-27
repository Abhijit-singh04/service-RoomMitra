using Microsoft.AspNetCore.Authorization;

namespace RoomMitra.Api.Security;

/// <summary>
/// Authorization attributes for role-based access control.
/// These work with Azure AD B2C roles configured in the token claims.
/// </summary>
public static class AuthorizationPolicies
{
    public const string RequireAuthenticated = "RequireAuthenticated";
    public const string RequireAdmin = "RequireAdmin";
    public const string RequireUser = "RequireUser";
}

/// <summary>
/// Attribute that requires a user to be authenticated.
/// </summary>
public class RequireAuthenticatedAttribute : AuthorizeAttribute
{
    public RequireAuthenticatedAttribute() : base(AuthorizationPolicies.RequireAuthenticated)
    {
    }
}

/// <summary>
/// Attribute that requires a user to have the Admin role.
/// </summary>
public class RequireAdminAttribute : AuthorizeAttribute
{
    public RequireAdminAttribute() : base(AuthorizationPolicies.RequireAdmin)
    {
    }
}

/// <summary>
/// Attribute that requires a user to have the User role.
/// </summary>
public class RequireUserAttribute : AuthorizeAttribute
{
    public RequireUserAttribute() : base(AuthorizationPolicies.RequireUser)
    {
    }
}
