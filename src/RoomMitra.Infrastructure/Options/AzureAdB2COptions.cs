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
    /// For CIAM: https://{tenant}.ciamlogin.com/{tenantId}/v2.0/
    /// For B2C: https://{tenant}.b2clogin.com/{tenantId}/v2.0/
    /// </summary>
    public string GetIssuer()
    {
        if (!string.IsNullOrEmpty(Issuer))
            return Issuer;

        // If no policy is set, assume CIAM (ciamlogin). Otherwise, B2C (b2clogin).
        var domain = string.IsNullOrEmpty(SignUpSignInPolicy) ? "ciamlogin.com" : "b2clogin.com";
        return $"https://{TenantName}.{domain}/{TenantId}/v2.0/";
    }

    /// <summary>
    /// Gets the constructed JWKS URI.
    /// </summary>
    public string GetJwksUri()
    {
        if (!string.IsNullOrEmpty(JwksUri))
            return JwksUri;

        if (string.IsNullOrEmpty(SignUpSignInPolicy))
        {
            // CIAM format
            return $"https://{TenantName}.ciamlogin.com/{TenantId}/discovery/v2.0/keys";
        }

        // B2C format
        return $"https://{TenantName}.b2clogin.com/{TenantName}.onmicrosoft.com/{SignUpSignInPolicy}/discovery/v2.0/keys";
    }

    /// <summary>
    /// Gets the OpenID Configuration metadata endpoint.
    /// </summary>
    public string GetMetadataAddress()
    {
        if (string.IsNullOrEmpty(SignUpSignInPolicy))
        {
            // CIAM format
            return $"https://{TenantName}.ciamlogin.com/{TenantId}/v2.0/.well-known/openid-configuration";
        }

        // B2C format
        return $"https://{TenantName}.b2clogin.com/{TenantName}.onmicrosoft.com/{SignUpSignInPolicy}/v2.0/.well-known/openid-configuration";
    }

    /// <summary>
    /// Gets the authority URL for Azure AD B2C.
    /// </summary>
    public string GetAuthority()
    {
        if (string.IsNullOrEmpty(SignUpSignInPolicy))
        {
            // CIAM format
            return $"https://{TenantName}.ciamlogin.com/{TenantId}";
        }

        // B2C format
        return $"https://{TenantName}.b2clogin.com/tfp/{TenantName}.onmicrosoft.com/{SignUpSignInPolicy}";
    }
}
