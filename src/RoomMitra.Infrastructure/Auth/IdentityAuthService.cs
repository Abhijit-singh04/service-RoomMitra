using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RoomMitra.Application.Abstractions.Auth;
using RoomMitra.Application.Abstractions.Notifications;
using RoomMitra.Application.Abstractions.Repositories;
using RoomMitra.Application.Models.Auth;
using RoomMitra.Application.Abstractions.Time;
using RoomMitra.Domain.Entities;
using RoomMitra.Infrastructure.Identity;
using RoomMitra.Infrastructure.Options;

#pragma warning disable CA5351

namespace RoomMitra.Infrastructure.Auth;

internal sealed class IdentityAuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly IOtpRequestRepository _otpRepository;
    private readonly ISmsSender _smsSender;
    private readonly IClock _clock;
    private readonly OtpOptions _otpOptions;

    public IdentityAuthService(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        ITokenService tokenService,
        IOtpRequestRepository otpRepository,
        ISmsSender smsSender,
        IClock clock,
        IOptions<OtpOptions> otpOptions)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _otpRepository = otpRepository;
        _smsSender = smsSender;
        _clock = clock;
        _otpOptions = otpOptions.Value;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
        {
            throw new InvalidOperationException("Email is already registered.");
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            UserName = request.Email.Trim().ToLowerInvariant()
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var msg = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException(msg);
        }

        var token = CreateToken(user);

        return new AuthResponse(
            token,
            new AuthUserDto(user.Id, user.Name, user.Email!, user.ProfileImageUrl)
        );
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email.Trim().ToLowerInvariant());
        if (user is null)
        {
            throw new InvalidOperationException("Invalid credentials.");
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException("Invalid credentials.");
        }

        var token = CreateToken(user);

        return new AuthResponse(
            token,
            new AuthUserDto(user.Id, user.Name, user.Email!, user.ProfileImageUrl)
        );
    }

    public async Task<RequestOtpResponse> RequestOtpAsync(RequestOtpRequest request, CancellationToken cancellationToken)
    {
        var phone = NormalizePhone(request.PhoneNumber);
        if (string.IsNullOrWhiteSpace(phone))
        {
            throw new InvalidOperationException("Phone number is required.");
        }

        var now = _clock.UtcNow;
        var latest = await _otpRepository.GetLatestByPhoneAsync(phone, cancellationToken);
        if (latest is not null && now - latest.LastSentAt < TimeSpan.FromSeconds(_otpOptions.ResendCooldownSeconds))
        {
            throw new InvalidOperationException("Please wait before requesting another code.");
        }

        var code = GenerateCode(_otpOptions.CodeLength);
        var salt = GenerateSalt();
        var hash = HashCode(code, salt);
        var requestId = Guid.NewGuid().ToString("N");

        var otpRequest = new OtpRequest
        {
            PhoneNumber = phone,
            RequestId = requestId,
            OtpHash = hash,
            Salt = salt,
            ExpiresAt = now.AddMinutes(_otpOptions.ExpiryMinutes),
            AttemptCount = 0,
            Used = false,
            LastSentAt = now,
            RequestIp = null // could be populated via middleware
        };

        await _otpRepository.AddAsync(otpRequest, cancellationToken);

        var message = $"RoomMitra code: {code}. Expires in {_otpOptions.ExpiryMinutes} minutes.";
        await _smsSender.SendAsync(phone, message, cancellationToken);

        return new RequestOtpResponse(requestId);
    }

    public async Task<AuthResponse> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken cancellationToken)
    {
        var phone = NormalizePhone(request.PhoneNumber);
        if (string.IsNullOrWhiteSpace(phone))
        {
            throw new InvalidOperationException("Phone number is required.");
        }

        var otpRequest = await _otpRepository.GetByRequestIdAsync(request.RequestId, cancellationToken);
        if (otpRequest is null || otpRequest.PhoneNumber != phone)
        {
            throw new InvalidOperationException("Invalid code.");
        }

        var now = _clock.UtcNow;
        if (otpRequest.Used)
        {
            throw new InvalidOperationException("Code has already been used.");
        }

        if (now > otpRequest.ExpiresAt)
        {
            throw new InvalidOperationException("Code has expired.");
        }

        if (otpRequest.AttemptCount >= _otpOptions.MaxAttempts)
        {
            throw new InvalidOperationException("Maximum attempts exceeded.");
        }

        var providedHash = HashCode(request.Code, otpRequest.Salt);
        if (!TimeConstantEquals(providedHash, otpRequest.OtpHash))
        {
            otpRequest.AttemptCount += 1;
            await _otpRepository.UpdateAsync(otpRequest, cancellationToken);
            throw new InvalidOperationException("Invalid code.");
        }

        otpRequest.Used = true;
        otpRequest.UsedAt = now;
        otpRequest.AttemptCount += 1;
        await _otpRepository.UpdateAsync(otpRequest, cancellationToken);

        var user = await _userManager.Users
            .Where(u => u.PhoneNumber == phone)
            .SingleOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            user = new AppUser
            {
                Id = Guid.NewGuid(),
                Name = $"User {MaskPhone(phone)}",
                PhoneNumber = phone,
                PhoneNumberConfirmed = true,
                Email = CreateSyntheticEmail(phone),
                UserName = phone,
                EmailConfirmed = false,
                IsVerified = true
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                var msg = string.Join("; ", createResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException(msg);
            }
        }
        else if (!user.PhoneNumberConfirmed)
        {
            user.PhoneNumberConfirmed = true;
            await _userManager.UpdateAsync(user);
        }

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
            new(ClaimTypes.Name, user.Name)
        };

        return _tokenService.CreateAccessToken(claims);
    }

    private static string NormalizePhone(string phone)
    {
        return phone.Trim().Replace(" ", string.Empty);
    }

    private static string GenerateCode(int length)
    {
        var digits = new char[length];
        var buffer = new byte[length];
        RandomNumberGenerator.Fill(buffer);
        for (var i = 0; i < length; i++)
        {
            digits[i] = (char)('0' + (buffer[i] % 10));
        }

        return new string(digits);
    }

    private static string GenerateSalt()
    {
        Span<byte> buffer = stackalloc byte[16];
        RandomNumberGenerator.Fill(buffer);
        return Convert.ToBase64String(buffer);
    }

    private static string HashCode(string code, string salt)
    {
        var data = Encoding.UTF8.GetBytes(code + salt);
        var hash = SHA256.HashData(data);
        return Convert.ToBase64String(hash);
    }

    private static bool TimeConstantEquals(string a, string b)
    {
        var aBytes = Encoding.UTF8.GetBytes(a);
        var bBytes = Encoding.UTF8.GetBytes(b);
        return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
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
}
