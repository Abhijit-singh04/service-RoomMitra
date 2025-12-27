using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using RoomMitra.Application.Abstractions.Auth;
using RoomMitra.Application.Models.Auth;
using RoomMitra.Infrastructure.Identity;

namespace RoomMitra.Infrastructure.Auth;

internal sealed class IdentityAuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly ITokenService _tokenService;

    public IdentityAuthService(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        ITokenService tokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
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
}
