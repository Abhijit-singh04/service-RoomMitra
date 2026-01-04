using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RoomMitra.Application.Abstractions.Auth;
using RoomMitra.Application.Models.Auth;
using RoomMitra.Infrastructure.Identity;

namespace RoomMitra.Infrastructure.Auth;

/// <summary>
/// Implementation of Firebase phone-based authentication.
/// This is an independent auth path from existing Google/Gmail/Azure AD flows.
/// </summary>
internal sealed class FirebasePhoneAuthService : IFirebasePhoneAuthService
{
    private readonly IFirebaseAuthService _firebaseAuth;
    private readonly UserManager<AppUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly ILogger<FirebasePhoneAuthService> _logger;

    public FirebasePhoneAuthService(
        IFirebaseAuthService firebaseAuth,
        UserManager<AppUser> userManager,
        ITokenService tokenService,
        ILogger<FirebasePhoneAuthService> logger)
    {
        _firebaseAuth = firebaseAuth;
        _userManager = userManager;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<FirebasePhoneVerifyResponse> VerifyPhoneAsync(
        FirebasePhoneVerifyRequest request,
        CancellationToken cancellationToken)
    {
        // Verify Firebase ID token
        var tokenResult = await _firebaseAuth.VerifyIdTokenAsync(request.FirebaseIdToken, cancellationToken);
        
        if (!tokenResult.IsValid || string.IsNullOrEmpty(tokenResult.PhoneNumber))
        {
            throw new InvalidOperationException("Invalid or expired Firebase token.");
        }

        var phone = NormalizePhone(tokenResult.PhoneNumber);

        // Check if user exists with this phone number
        var existingUser = await _userManager.Users
            .Where(u => u.PhoneNumber == phone)
            .SingleOrDefaultAsync(cancellationToken);

        return new FirebasePhoneVerifyResponse(
            UserExists: existingUser is not null,
            PhoneNumber: phone,
            Username: existingUser?.UserName
        );
    }

    public async Task<AuthResponse> RegisterWithPhoneAsync(
        FirebasePhoneRegisterRequest request,
        CancellationToken cancellationToken)
    {
        // Validate request
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            throw new InvalidOperationException("Username is required.");
        }

        // Verify Firebase ID token
        var tokenResult = await _firebaseAuth.VerifyIdTokenAsync(request.FirebaseIdToken, cancellationToken);
        
        if (!tokenResult.IsValid || string.IsNullOrEmpty(tokenResult.PhoneNumber))
        {
            throw new InvalidOperationException("Invalid or expired Firebase token.");
        }

        var phone = NormalizePhone(tokenResult.PhoneNumber);
        var username = request.Username.Trim().ToLowerInvariant();

        // Check if phone is already registered
        var existingByPhone = await _userManager.Users
            .Where(u => u.PhoneNumber == phone)
            .SingleOrDefaultAsync(cancellationToken);

        if (existingByPhone is not null)
        {
            throw new InvalidOperationException("This phone number is already registered. Please login instead.");
        }

        // Check if username is already taken
        var existingByUsername = await _userManager.FindByNameAsync(username);
        if (existingByUsername is not null)
        {
            throw new InvalidOperationException("Username is already taken. Please choose a different one.");
        }

        // Create new user
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Name = request.Username.Trim(), // Use username as display name initially
            UserName = username,
            PhoneNumber = phone,
            PhoneNumberConfirmed = true, // Phone verified via Firebase OTP
            Email = CreateSyntheticEmail(phone),
            EmailConfirmed = false,
            IsVerified = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Generate a random password since ASP.NET Identity requires one
        // User will authenticate via OTP, not password
        var randomPassword = GenerateRandomPassword();

        var createResult = await _userManager.CreateAsync(user, randomPassword);
        if (!createResult.Succeeded)
        {
            var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
            _logger.LogWarning("User creation failed: {Errors}", errors);
            throw new InvalidOperationException(errors);
        }

        _logger.LogInformation(
            "New user registered via Firebase phone auth. UserId: {UserId}, Phone: {Phone}",
            user.Id, MaskPhone(phone));

        var token = CreateToken(user);

        return new AuthResponse(
            token,
            new AuthUserDto(user.Id, user.Name, user.Email ?? string.Empty, user.ProfileImageUrl)
        );
    }

    public async Task<AuthResponse> LoginWithPhoneAsync(
        FirebasePhoneVerifyRequest request,
        CancellationToken cancellationToken)
    {
        // Verify Firebase ID token
        var tokenResult = await _firebaseAuth.VerifyIdTokenAsync(request.FirebaseIdToken, cancellationToken);
        
        if (!tokenResult.IsValid || string.IsNullOrEmpty(tokenResult.PhoneNumber))
        {
            throw new InvalidOperationException("Invalid or expired Firebase token.");
        }

        var phone = NormalizePhone(tokenResult.PhoneNumber);

        // Find user by phone number
        var user = await _userManager.Users
            .Where(u => u.PhoneNumber == phone)
            .SingleOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            throw new InvalidOperationException("No account found with this phone number. Please sign up first.");
        }

        // Check if account is locked
        if (await _userManager.IsLockedOutAsync(user))
        {
            throw new InvalidOperationException("Account is locked. Please try again later.");
        }

        // Reset access failed count on successful login
        await _userManager.ResetAccessFailedCountAsync(user);

        _logger.LogInformation("User logged in via Firebase phone OTP. UserId: {UserId}", user.Id);

        var token = CreateToken(user);

        return new AuthResponse(
            token,
            new AuthUserDto(user.Id, user.Name, user.Email ?? string.Empty, user.ProfileImageUrl)
        );
    }

    public async Task<AuthResponse> ResetPasswordAsync(
        FirebasePasswordResetRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            throw new InvalidOperationException("New password is required.");
        }

        // Verify Firebase ID token
        var tokenResult = await _firebaseAuth.VerifyIdTokenAsync(request.FirebaseIdToken, cancellationToken);
        
        if (!tokenResult.IsValid || string.IsNullOrEmpty(tokenResult.PhoneNumber))
        {
            throw new InvalidOperationException("Invalid or expired Firebase token.");
        }

        var phone = NormalizePhone(tokenResult.PhoneNumber);

        // Find user by phone number
        var user = await _userManager.Users
            .Where(u => u.PhoneNumber == phone)
            .SingleOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            throw new InvalidOperationException("No account found with this phone number.");
        }

        // Generate password reset token and reset password
        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, request.NewPassword);

        if (!resetResult.Succeeded)
        {
            var errors = string.Join("; ", resetResult.Errors.Select(e => e.Description));
            _logger.LogWarning("Password reset failed: {Errors}", errors);
            throw new InvalidOperationException(errors);
        }

        // Unlock account if it was locked
        if (await _userManager.IsLockedOutAsync(user))
        {
            await _userManager.SetLockoutEndDateAsync(user, null);
        }

        _logger.LogInformation("Password reset successful. UserId: {UserId}", user.Id);

        var token = CreateToken(user);

        return new AuthResponse(
            token,
            new AuthUserDto(user.Id, user.Name, user.Email ?? string.Empty, user.ProfileImageUrl)
        );
    }

    private string CreateToken(AppUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.Name),
            new("phone", user.PhoneNumber ?? string.Empty),
            new("auth_method", "firebase_phone") // Track auth method
        };

        return _tokenService.CreateAccessToken(claims);
    }

    private static string NormalizePhone(string phone)
    {
        return phone.Trim().Replace(" ", string.Empty);
    }

    private static string CreateSyntheticEmail(string phone)
    {
        var sanitized = phone.Replace("+", string.Empty);
        return $"{sanitized}@phone.roommitra";
    }

    private static string MaskPhone(string phone)
    {
        if (phone.Length <= 4)
        {
            return phone;
        }

        var suffix = phone[^4..];
        return new string('*', Math.Max(0, phone.Length - 4)) + suffix;
    }

    private static string GenerateRandomPassword()
    {
        // Generate a secure random password that meets ASP.NET Identity requirements
        // This password is not used for login - users authenticate via OTP
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%";
        var random = new Random();
        var password = new char[24];
        for (int i = 0; i < password.Length; i++)
        {
            password[i] = chars[random.Next(chars.Length)];
        }
        // Ensure it has required characters
        password[0] = 'A'; // Uppercase
        password[1] = 'a'; // Lowercase
        password[2] = '1'; // Digit
        password[3] = '!'; // Special
        return new string(password);
    }
}
