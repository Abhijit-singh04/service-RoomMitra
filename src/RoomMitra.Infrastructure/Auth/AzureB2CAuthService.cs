using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RoomMitra.Application.Abstractions.Auth;
using RoomMitra.Application.Models.Auth;
using RoomMitra.Infrastructure.Options;

namespace RoomMitra.Infrastructure.Auth;

/// <summary>
/// Azure AD B2C authentication service implementing the BFF pattern.
/// Handles OAuth 2.0 Authorization Code flow with PKCE on the server side.
/// </summary>
internal sealed class AzureB2CAuthService : IAzureAuthService, IDisposable
{
    private readonly AzureAdB2COptions _options;
    private readonly HttpClient _httpClient;
    private readonly ILogger<AzureB2CAuthService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public AzureB2CAuthService(
        IOptions<AzureAdB2COptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<AzureB2CAuthService> logger)
    {
        _options = options.Value;
        _httpClient = httpClientFactory.CreateClient("AzureB2C");
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };
    }

    public Task<AzureLoginResponse> GetAuthorizationUrlAsync(
        string redirectUri,
        string codeChallenge,
        string returnUrl,
        CancellationToken cancellationToken = default)
    {
        // Generate state for CSRF protection
        var state = GenerateState(returnUrl);
        
        // Build authorization URL
        var authUrl = BuildAuthorizationUrl(redirectUri, codeChallenge, state);
        
        return Task.FromResult(new AzureLoginResponse(authUrl, state));
    }

    public async Task<AzureAuthResponse> ExchangeCodeForTokensAsync(
        AzureCallbackRequest request,
        CancellationToken cancellationToken = default)
    {
        var tokenEndpoint = GetTokenEndpoint();
        
        var tokenRequest = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = _options.ClientId,
            ["code"] = request.Code,
            ["redirect_uri"] = request.RedirectUri,
            ["code_verifier"] = request.CodeVerifier
        };

        // Add client secret if configured (confidential client)
        if (!string.IsNullOrEmpty(_options.ClientSecret))
        {
            tokenRequest["client_secret"] = _options.ClientSecret;
        }

        // Add scopes
        var scopes = GetScopes();
        tokenRequest["scope"] = string.Join(" ", scopes);

        var content = new FormUrlEncodedContent(tokenRequest);
        
        _logger.LogDebug("Exchanging authorization code for tokens at {Endpoint}", tokenEndpoint);
        
        var response = await _httpClient.PostAsync(tokenEndpoint, content, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Token exchange failed: {StatusCode} - {Body}", response.StatusCode, responseBody);
            throw new InvalidOperationException($"Token exchange failed: {response.StatusCode}");
        }

        var tokenResponse = JsonSerializer.Deserialize<TokenEndpointResponse>(responseBody, _jsonOptions);
        if (tokenResponse is null)
        {
            throw new InvalidOperationException("Failed to deserialize token response");
        }

        // Extract user info from ID token
        var user = ExtractUserFromIdToken(tokenResponse.IdToken);

        return new AzureAuthResponse(
            tokenResponse.AccessToken,
            tokenResponse.IdToken,
            tokenResponse.RefreshToken,
            tokenResponse.ExpiresIn,
            user
        );
    }

    public async Task<AzureAuthResponse> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var tokenEndpoint = GetTokenEndpoint();
        
        var tokenRequest = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = _options.ClientId,
            ["refresh_token"] = refreshToken
        };

        // Add client secret if configured
        if (!string.IsNullOrEmpty(_options.ClientSecret))
        {
            tokenRequest["client_secret"] = _options.ClientSecret;
        }

        // Add scopes
        var scopes = GetScopes();
        tokenRequest["scope"] = string.Join(" ", scopes);

        var content = new FormUrlEncodedContent(tokenRequest);
        
        _logger.LogDebug("Refreshing token at {Endpoint}", tokenEndpoint);
        
        var response = await _httpClient.PostAsync(tokenEndpoint, content, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Token refresh failed: {StatusCode} - {Body}", response.StatusCode, responseBody);
            throw new InvalidOperationException($"Token refresh failed: {response.StatusCode}");
        }

        var tokenResponse = JsonSerializer.Deserialize<TokenEndpointResponse>(responseBody, _jsonOptions);
        if (tokenResponse is null)
        {
            throw new InvalidOperationException("Failed to deserialize token response");
        }

        // Extract user info from ID token
        var user = ExtractUserFromIdToken(tokenResponse.IdToken);

        return new AzureAuthResponse(
            tokenResponse.AccessToken,
            tokenResponse.IdToken,
            tokenResponse.RefreshToken,
            tokenResponse.ExpiresIn,
            user
        );
    }

    public Task<AzureUserDto?> ValidateTokenAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(accessToken))
            {
                return Task.FromResult<AzureUserDto?>(null);
            }

            var token = handler.ReadJwtToken(accessToken);
            
            // Check expiration
            if (token.ValidTo < DateTime.UtcNow)
            {
                return Task.FromResult<AzureUserDto?>(null);
            }

            var user = new AzureUserDto(
                Id: token.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? string.Empty,
                ObjectId: token.Claims.FirstOrDefault(c => c.Type == "oid")?.Value,
                Name: token.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? 
                      $"{token.Claims.FirstOrDefault(c => c.Type == "given_name")?.Value} {token.Claims.FirstOrDefault(c => c.Type == "family_name")?.Value}".Trim(),
                Email: token.Claims.FirstOrDefault(c => c.Type == "email")?.Value ?? 
                       token.Claims.FirstOrDefault(c => c.Type == "emails")?.Value ?? string.Empty,
                IdentityProvider: token.Claims.FirstOrDefault(c => c.Type == "idp")?.Value ?? "local"
            );

            return Task.FromResult<AzureUserDto?>(user);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return Task.FromResult<AzureUserDto?>(null);
        }
    }

    public string GetLogoutUrl(string postLogoutRedirectUri)
    {
        var logoutEndpoint = $"https://{_options.TenantName}.b2clogin.com/{_options.TenantName}.onmicrosoft.com/{_options.SignUpSignInPolicy}/oauth2/v2.0/logout";
        return $"{logoutEndpoint}?post_logout_redirect_uri={Uri.EscapeDataString(postLogoutRedirectUri)}";
    }

    private string BuildAuthorizationUrl(string redirectUri, string codeChallenge, string state)
    {
        var baseUrl = $"https://{_options.TenantName}.b2clogin.com/{_options.TenantName}.onmicrosoft.com/{_options.SignUpSignInPolicy}/oauth2/v2.0/authorize";
        var scopes = GetScopes();

        var queryParams = new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["response_type"] = "code",
            ["redirect_uri"] = redirectUri,
            ["scope"] = string.Join(" ", scopes),
            ["state"] = state,
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256",
            ["response_mode"] = "query"
        };

        var queryString = string.Join("&", queryParams.Select(kvp => 
            $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

        return $"{baseUrl}?{queryString}";
    }

    private string GetTokenEndpoint()
    {
        return $"https://{_options.TenantName}.b2clogin.com/{_options.TenantName}.onmicrosoft.com/{_options.SignUpSignInPolicy}/oauth2/v2.0/token";
    }

    private List<string> GetScopes()
    {
        var scopes = new List<string> { "openid", "profile", "email", "offline_access" };
        
        if (!string.IsNullOrEmpty(_options.ApiScope))
        {
            scopes.Add(_options.ApiScope);
        }

        return scopes;
    }

    private static string GenerateState(string returnUrl)
    {
        var stateData = new { returnUrl, nonce = Guid.NewGuid().ToString("N") };
        var json = JsonSerializer.Serialize(stateData);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    private AzureUserDto ExtractUserFromIdToken(string? idToken)
    {
        if (string.IsNullOrEmpty(idToken))
        {
            return new AzureUserDto(
                Id: string.Empty,
                ObjectId: null,
                Name: "Unknown",
                Email: string.Empty,
                IdentityProvider: null
            );
        }

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(idToken);

            return new AzureUserDto(
                Id: token.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? string.Empty,
                ObjectId: token.Claims.FirstOrDefault(c => c.Type == "oid")?.Value,
                Name: token.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? 
                      $"{token.Claims.FirstOrDefault(c => c.Type == "given_name")?.Value} {token.Claims.FirstOrDefault(c => c.Type == "family_name")?.Value}".Trim(),
                Email: token.Claims.FirstOrDefault(c => c.Type == "email")?.Value ?? 
                       token.Claims.FirstOrDefault(c => c.Type == "emails")?.Value ?? string.Empty,
                IdentityProvider: token.Claims.FirstOrDefault(c => c.Type == "idp")?.Value ?? "local"
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract user from ID token");
            return new AzureUserDto(
                Id: string.Empty,
                ObjectId: null,
                Name: "Unknown",
                Email: string.Empty,
                IdentityProvider: null
            );
        }
    }

    public void Dispose()
    {
        // HttpClient is managed by IHttpClientFactory, no need to dispose
    }

    /// <summary>
    /// Internal class for deserializing token endpoint response.
    /// </summary>
    private sealed class TokenEndpointResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; init; } = string.Empty;

        [JsonPropertyName("id_token")]
        public string? IdToken { get; init; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; init; }

        [JsonPropertyName("token_type")]
        public string TokenType { get; init; } = "Bearer";

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; init; }

        [JsonPropertyName("scope")]
        public string? Scope { get; init; }
    }
}
