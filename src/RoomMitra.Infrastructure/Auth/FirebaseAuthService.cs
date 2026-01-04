using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RoomMitra.Application.Abstractions.Auth;
using RoomMitra.Infrastructure.Options;

namespace RoomMitra.Infrastructure.Auth;

/// <summary>
/// Firebase authentication service for verifying ID tokens.
/// Uses Firebase Admin SDK for production token verification.
/// </summary>
internal sealed class FirebaseAuthService : IFirebaseAuthService
{
    private readonly FirebaseOptions _options;
    private readonly ILogger<FirebaseAuthService> _logger;
    private static bool _firebaseInitialized = false;
    private static readonly object _initLock = new();

    public FirebaseAuthService(
        IOptions<FirebaseOptions> options,
        ILogger<FirebaseAuthService> logger)
    {
        _options = options.Value;
        _logger = logger;
        
        InitializeFirebase();
    }

    private void InitializeFirebase()
    {
        if (_firebaseInitialized) return;

        lock (_initLock)
        {
            if (_firebaseInitialized) return;

            try
            {
                if (FirebaseApp.DefaultInstance != null)
                {
                    _firebaseInitialized = true;
                    return;
                }

                FirebaseApp app;
                
                if (!string.IsNullOrEmpty(_options.ServiceAccountPath) && File.Exists(_options.ServiceAccountPath))
                {
                    // Use service account file if provided
                    app = FirebaseApp.Create(new AppOptions
                    {
                        Credential = GoogleCredential.FromFile(_options.ServiceAccountPath),
                        ProjectId = _options.ProjectId
                    });
                    _logger.LogInformation("Firebase initialized with service account file: {Path}", _options.ServiceAccountPath);
                }
                else if (!string.IsNullOrEmpty(_options.ProjectId))
                {
                    // Initialize with just project ID - uses default credentials or environment
                    app = FirebaseApp.Create(new AppOptions
                    {
                        ProjectId = _options.ProjectId
                    });
                    _logger.LogInformation("Firebase initialized with project ID: {ProjectId}", _options.ProjectId);
                }
                else
                {
                    _logger.LogWarning("Firebase not configured. Set Firebase:ProjectId and optionally Firebase:ServiceAccountPath");
                    return;
                }

                _firebaseInitialized = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Firebase Admin SDK");
            }
        }
    }

    public async Task<FirebaseTokenResult> VerifyIdTokenAsync(string idToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            return new FirebaseTokenResult(
                Uid: string.Empty,
                PhoneNumber: null,
                Email: null,
                IsValid: false
            );
        }

        // Simulation mode for development/testing
        if (_options.SimulateVerification)
        {
            _logger.LogWarning("Firebase token verification is in simulation mode. Do not use in production!");
            return SimulateTokenVerification(idToken);
        }

        // Production mode: Use Firebase Admin SDK
        return await VerifyWithFirebaseAdminAsync(idToken, cancellationToken);
    }

    /// <summary>
    /// Simulates token verification for development/testing.
    /// Accepts both simulated tokens and real Firebase tokens (extracts claims without verification).
    /// </summary>
    private FirebaseTokenResult SimulateTokenVerification(string idToken)
    {
        // For simulation, accept tokens in format: "simulated:{uid}:{phoneNumber}"
        if (idToken.StartsWith("simulated:", StringComparison.OrdinalIgnoreCase))
        {
            var parts = idToken.Split(':');
            if (parts.Length >= 3)
            {
                var uid = parts[1];
                var phoneNumber = parts[2];
                
                _logger.LogInformation(
                    "Simulated Firebase token verified. UID: {Uid}, Phone: {Phone}",
                    uid, phoneNumber);

                return new FirebaseTokenResult(
                    Uid: uid,
                    PhoneNumber: phoneNumber,
                    Email: null,
                    IsValid: true
                );
            }
        }

        // Try to decode real Firebase JWT token (without signature verification)
        // This is for testing with Firebase test phone numbers
        try
        {
            var tokenParts = idToken.Split('.');
            if (tokenParts.Length == 3)
            {
                // Decode the payload (second part)
                var payload = tokenParts[1];
                // Add padding if needed for Base64
                var paddedPayload = payload.Length % 4 == 0 
                    ? payload 
                    : payload + new string('=', 4 - payload.Length % 4);
                // Replace URL-safe characters
                paddedPayload = paddedPayload.Replace('-', '+').Replace('_', '/');
                
                var payloadBytes = Convert.FromBase64String(paddedPayload);
                var payloadJson = System.Text.Encoding.UTF8.GetString(payloadBytes);
                
                // Parse JSON to extract claims
                using var doc = System.Text.Json.JsonDocument.Parse(payloadJson);
                var root = doc.RootElement;
                
                var uid = root.TryGetProperty("user_id", out var userIdProp) 
                    ? userIdProp.GetString() 
                    : root.TryGetProperty("sub", out var subProp) 
                        ? subProp.GetString() 
                        : $"firebase_{idToken.GetHashCode():X8}";
                
                var phoneNumber = root.TryGetProperty("phone_number", out var phoneProp) 
                    ? phoneProp.GetString() 
                    : "+919999999999";
                
                var email = root.TryGetProperty("email", out var emailProp) 
                    ? emailProp.GetString() 
                    : null;
                
                _logger.LogInformation(
                    "Firebase token decoded (simulation mode). UID: {Uid}, Phone: {Phone}",
                    uid, phoneNumber);

                return new FirebaseTokenResult(
                    Uid: uid ?? $"firebase_{idToken.GetHashCode():X8}",
                    PhoneNumber: phoneNumber,
                    Email: email,
                    IsValid: true
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to decode Firebase token, using fallback");
        }

        // Fallback: Generate a deterministic UID from the token
        var simulatedUid = $"firebase_{idToken.GetHashCode():X8}";
        
        _logger.LogInformation(
            "Simulated Firebase token verification with generated UID: {Uid}",
            simulatedUid);

        return new FirebaseTokenResult(
            Uid: simulatedUid,
            PhoneNumber: "+919999999999",
            Email: null,
            IsValid: true
        );
    }

    /// <summary>
    /// Verifies the token using Firebase Admin SDK.
    /// </summary>
    private async Task<FirebaseTokenResult> VerifyWithFirebaseAdminAsync(string idToken, CancellationToken cancellationToken)
    {
        try
        {
            if (!_firebaseInitialized || FirebaseApp.DefaultInstance == null)
            {
                _logger.LogError("Firebase Admin SDK not initialized. Cannot verify token.");
                return new FirebaseTokenResult(
                    Uid: string.Empty,
                    PhoneNumber: null,
                    Email: null,
                    IsValid: false
                );
            }

            // Verify the ID token using Firebase Admin SDK
            var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken, cancellationToken);

            // Extract phone number and email from claims
            string? phoneNumber = null;
            string? email = null;

            if (decodedToken.Claims.TryGetValue("phone_number", out var phoneObj))
            {
                phoneNumber = phoneObj?.ToString();
            }

            if (decodedToken.Claims.TryGetValue("email", out var emailObj))
            {
                email = emailObj?.ToString();
            }

            _logger.LogInformation(
                "Firebase token verified successfully. UID: {Uid}, Phone: {Phone}, Email: {Email}",
                decodedToken.Uid, phoneNumber ?? "N/A", email ?? "N/A");

            return new FirebaseTokenResult(
                Uid: decodedToken.Uid,
                PhoneNumber: phoneNumber,
                Email: email,
                IsValid: true
            );
        }
        catch (FirebaseAuthException ex)
        {
            _logger.LogWarning(ex, "Firebase token verification failed: {Message}", ex.Message);
            return new FirebaseTokenResult(
                Uid: string.Empty,
                PhoneNumber: null,
                Email: null,
                IsValid: false
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify Firebase ID token");
            return new FirebaseTokenResult(
                Uid: string.Empty,
                PhoneNumber: null,
                Email: null,
                IsValid: false
            );
        }
    }
}
