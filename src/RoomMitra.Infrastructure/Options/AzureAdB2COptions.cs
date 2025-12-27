namespace RoomMitra.Infrastructure.Options;

/// <summary>
/// Configuration options for Azure AD B2C authentication.
/// These values should be configured in Azure Key Vault or appsettings.json.
/// </summary>
public sealed class AzureAdB2COptions
{
    public const string SectionName = "AzureAdB2C";

    /// <summary>
    /// The Azure AD B2C tenant name (e.g., "your-tenant").
    /// </summary>
    public string TenantName { get; init; } = string.Empty;

    /// <summary>
    /// The Azure AD B2C tenant ID (GUID).
    /// </summary>
    public string TenantId { get; init; } = string.Empty;

    /// <summary>
    /// The user flow / policy name (e.g., "B2C_1_signupsignin").
    /// </summary>
    public string SignUpSignInPolicy { get; init; } = string.Empty;

    /// <summary>
    /// The client ID of the API application registration in Azure AD B2C.
    /// This is the "audience" for token validation.
    /// </summary>
    public string ClientId { get; init; } = string.Empty;

    /// <summary>
    /// The expected issuer URL for token validation.
    /// Format: https://{tenant}.b2clogin.com/{tenantId}/v2.0/
    /// If empty, it will be constructed from TenantName and TenantId.
    /// </summary>
    public string Issuer { get; init; } = string.Empty;

    /// <summary>
    /// The JWKS (JSON Web Key Set) URI for token signature validation.
    /// If empty, it will be constructed from TenantName and SignUpSignInPolicy.
    /// </summary>
    public string JwksUri { get; init; } = string.Empty;

    /// <summary>
    /// Gets the constructed issuer URL.
    /// </summary>
    public string GetIssuer()
    {
        if (!string.IsNullOrEmpty(Issuer))
            return Issuer;

        return $"https://{TenantName}.b2clogin.com/{TenantId}/v2.0/";
    }

    /// <summary>
    /// Gets the constructed JWKS URI.
    /// </summary>
    public string GetJwksUri()
    {
        if (!string.IsNullOrEmpty(JwksUri))
            return JwksUri;

        return $"https://{TenantName}.b2clogin.com/{TenantName}.onmicrosoft.com/{SignUpSignInPolicy}/discovery/v2.0/keys";
    }

    /// <summary>
    /// Gets the OpenID Configuration metadata endpoint.
    /// </summary>
    public string GetMetadataAddress()
    {
        return $"https://{TenantName}.b2clogin.com/{TenantName}.onmicrosoft.com/{SignUpSignInPolicy}/v2.0/.well-known/openid-configuration";
    }

    /// <summary>
    /// Gets the authority URL for Azure AD B2C.
    /// </summary>
    public string GetAuthority()
    {
        return $"https://{TenantName}.b2clogin.com/tfp/{TenantName}.onmicrosoft.com/{SignUpSignInPolicy}";
    }
}
