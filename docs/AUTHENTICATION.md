# RoomMitra API - Authentication & Authorization

## Overview

The RoomMitra API uses **Azure AD B2C** for authentication. The backend validates JWT tokens issued by Azure AD B2C and extracts user identity from the token claims.

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    Token Validation Flow                         │
└─────────────────────────────────────────────────────────────────┘

    Incoming Request
    Authorization: Bearer <jwt-token>
         │
         ▼
    ┌────────────────────────────────────────┐
    │         JWT Bearer Middleware          │
    │                                        │
    │  1. Parse token from header            │
    │  2. Fetch Azure AD B2C public keys     │
    │     (JWKS endpoint, cached)            │
    │  3. Validate signature (RS256)         │
    │  4. Validate claims:                   │
    │     - iss (issuer)                     │
    │     - aud (audience)                   │
    │     - exp (expiry)                     │
    │     - nbf (not before)                 │
    │  5. Create ClaimsPrincipal             │
    └────────────────────────────────────────┘
         │
         ▼
    ┌────────────────────────────────────────┐
    │        IUserContext Service            │
    │                                        │
    │  Extract from claims:                  │
    │  - UserId (oid or sub)                 │
    │  - Email                               │
    │  - Name                                │
    │  - Roles                               │
    │  - IdentityProvider                    │
    └────────────────────────────────────────┘
         │
         ▼
    ┌────────────────────────────────────────┐
    │          Controller Action             │
    │                                        │
    │  [Authorize] attribute enforces auth   │
    │  IUserContext injected for user data   │
    └────────────────────────────────────────┘
```

## Configuration

### appsettings.json

```json
{
  "AzureAdB2C": {
    "TenantName": "your-tenant-name",
    "TenantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
    "SignUpSignInPolicy": "B2C_1_signupsignin",
    "ClientId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
  }
}
```

| Setting | Description |
|---------|-------------|
| `TenantName` | Your Azure AD B2C tenant name (e.g., `roommitra`) |
| `TenantId` | The GUID of your B2C tenant |
| `SignUpSignInPolicy` | User flow name (e.g., `B2C_1_signupsignin`) |
| `ClientId` | The API application's Client ID from Azure |

### Configuration Class

```csharp
// AzureAdB2COptions.cs
public sealed class AzureAdB2COptions
{
    public string TenantName { get; init; }
    public string TenantId { get; init; }
    public string SignUpSignInPolicy { get; init; }
    public string ClientId { get; init; }
    
    // Computed properties
    public string GetIssuer() => $"https://{TenantName}.b2clogin.com/{TenantId}/v2.0/";
    public string GetMetadataAddress() => $"https://{TenantName}.b2clogin.com/{TenantName}.onmicrosoft.com/{SignUpSignInPolicy}/v2.0/.well-known/openid-configuration";
}
```

## JWT Token Validation

### What Gets Validated

| Validation | Description |
|------------|-------------|
| **Signature** | Token signed by Azure AD B2C private key, validated using public key from JWKS endpoint |
| **Issuer (iss)** | Must match `https://{tenant}.b2clogin.com/{tenantId}/v2.0/` |
| **Audience (aud)** | Must match API's Client ID |
| **Expiry (exp)** | Token must not be expired |
| **Not Before (nbf)** | Token must be valid (issued time) |

### Token Claims

Azure AD B2C tokens contain these claims:

```json
{
  "sub": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
  "oid": "ffffffff-gggg-hhhh-iiii-jjjjjjjjjjjj",
  "name": "John Doe",
  "emails": ["john@example.com"],
  "idp": "google.com",
  "tfp": "B2C_1_signupsignin",
  "aud": "your-api-client-id",
  "iss": "https://tenant.b2clogin.com/tenant-id/v2.0/",
  "iat": 1735344000,
  "exp": 1735347600,
  "nbf": 1735344000
}
```

## Using Authentication

### Protected Endpoints

Use `[Authorize]` attribute:

```csharp
[ApiController]
[Route("api/rooms")]
public class RoomsController : ControllerBase
{
    [HttpGet]
    public IActionResult GetRooms() 
    {
        // Public endpoint - no auth required
        return Ok(rooms);
    }
    
    [Authorize]
    [HttpPost]
    public IActionResult CreateRoom([FromBody] CreateRoomDto dto)
    {
        // Requires valid JWT token
        return Ok();
    }
}
```

### Accessing User Identity

Inject `IUserContext`:

```csharp
public class RoomsController : ControllerBase
{
    private readonly IUserContext _userContext;
    
    public RoomsController(IUserContext userContext)
    {
        _userContext = userContext;
    }
    
    [Authorize]
    [HttpPost]
    public IActionResult CreateRoom([FromBody] CreateRoomDto dto)
    {
        // Get user info from token
        var userId = _userContext.UserId;
        var email = _userContext.Email;
        var name = _userContext.Name;
        var idp = _userContext.IdentityProvider; // "google.com" or "local"
        
        // Create room with owner
        var room = new Room 
        { 
            OwnerId = userId.Value,
            OwnerEmail = email 
        };
        
        return Ok(room);
    }
}
```

### Role-Based Authorization

```csharp
// Using policy
[Authorize(Policy = "RequireAdmin")]
[HttpDelete("{id}")]
public IActionResult DeleteRoom(Guid id)
{
    // Only admins can delete
    return NoContent();
}

// Using IUserContext
[Authorize]
[HttpPut("{id}")]
public IActionResult UpdateRoom(Guid id, [FromBody] UpdateRoomDto dto)
{
    if (!_userContext.HasRole("Admin") && !IsOwner(id))
    {
        return Forbid();
    }
    
    return Ok();
}
```

## IUserContext Interface

```csharp
public interface IUserContext
{
    /// <summary>User's unique ID (from oid or sub claim)</summary>
    Guid? UserId { get; }
    
    /// <summary>Azure AD B2C Object ID</summary>
    string? ObjectId { get; }
    
    /// <summary>Subject identifier</summary>
    string? Subject { get; }
    
    /// <summary>User's email</summary>
    string? Email { get; }
    
    /// <summary>User's display name</summary>
    string? Name { get; }
    
    /// <summary>Identity provider (e.g., "google.com", "local")</summary>
    string? IdentityProvider { get; }
    
    /// <summary>User's roles</summary>
    IReadOnlyList<string> Roles { get; }
    
    /// <summary>Whether user is authenticated</summary>
    bool IsAuthenticated { get; }
    
    /// <summary>Check if user has a role</summary>
    bool HasRole(string role);
}
```

## Authorization Policies

Defined in `Program.cs`:

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAuthenticated", policy =>
        policy.RequireAuthenticatedUser());
    
    options.AddPolicy("RequireAdmin", policy =>
        policy.RequireRole("Admin", "Administrator"));
    
    options.AddPolicy("RequireUser", policy =>
        policy.RequireRole("User", "Admin"));
});
```

## Error Responses

| Status | Scenario |
|--------|----------|
| **401 Unauthorized** | No token, invalid token, expired token |
| **403 Forbidden** | Valid token but insufficient permissions |

Example 401 response:
```http
HTTP/1.1 401 Unauthorized
WWW-Authenticate: Bearer error="invalid_token", error_description="The token is expired"
```

## Debugging

### View Token Claims

```csharp
[HttpGet("debug-claims")]
[Authorize]
public IActionResult DebugClaims()
{
    var claims = User.Claims
        .Select(c => new { c.Type, c.Value })
        .ToList();
    
    return Ok(claims);
}
```

### Logging Configuration

JWT events are logged automatically:

```csharp
options.Events = new JwtBearerEvents
{
    OnAuthenticationFailed = context =>
    {
        logger.LogWarning("Auth failed: {Error}", context.Exception.Message);
        return Task.CompletedTask;
    },
    OnTokenValidated = context =>
    {
        logger.LogDebug("Token validated for: {Sub}", 
            context.Principal?.FindFirst("sub")?.Value);
        return Task.CompletedTask;
    }
};
```

## CORS Configuration

Credentials are enabled for cookie-based auth from the frontend:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
        policy
            .WithOrigins("http://localhost:3000", "https://yourdomain.com")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials() // Required for cookies
    );
});
```

## Testing with Swagger

1. Get a token from the frontend (check browser cookies/network tab)
2. In Swagger UI, click "Authorize"
3. Enter: `Bearer <your-token>`
4. Test protected endpoints

## Security Best Practices

1. **Never disable token validation in production**
2. **Use HTTPS in production**
3. **Keep tokens short-lived** (Azure default: 1 hour)
4. **Use refresh tokens for long sessions**
5. **Implement proper CORS policies**
6. **Log authentication failures for monitoring**
7. **Use Azure Key Vault for secrets in production**

## Troubleshooting

### "Bearer error=invalid_token"

- Check token expiration
- Verify audience matches ClientId
- Ensure issuer URL is correct

### "IDX10214: Audience validation failed"

- Verify `ClientId` in config matches token's `aud` claim
- Check for typos in configuration

### "IDX10511: Signature validation failed"

- Azure AD B2C key rotation may have occurred
- Restart application to refresh JWKS cache
- Verify policy name is correct

### Token not being sent

- Check CORS `AllowCredentials()` is enabled
- Verify frontend sends `credentials: 'include'`
- Check cookie SameSite settings
