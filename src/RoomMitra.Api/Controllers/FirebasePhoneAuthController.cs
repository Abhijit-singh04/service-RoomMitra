using Microsoft.AspNetCore.Mvc;
using RoomMitra.Application.Abstractions.Auth;
using RoomMitra.Application.Models.Auth;

namespace RoomMitra.Api.Controllers;

/// <summary>
/// Controller for Firebase phone OTP-based authentication.
/// This is an independent auth path from existing Google/Gmail/Azure AD flows.
/// </summary>
[ApiController]
[Route("api/auth/firebase-phone")]
public sealed class FirebasePhoneAuthController : ControllerBase
{
    private readonly IFirebasePhoneAuthService _firebasePhoneAuthService;

    public FirebasePhoneAuthController(IFirebasePhoneAuthService firebasePhoneAuthService)
    {
        _firebasePhoneAuthService = firebasePhoneAuthService;
    }

    /// <summary>
    /// Verifies a Firebase ID token and checks if the phone number is already registered.
    /// Call this after successful Firebase OTP verification in the frontend.
    /// </summary>
    /// <param name="request">The Firebase token verification request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Response indicating if user exists and the phone number.</returns>
    [HttpPost("verify")]
    [ProducesResponseType(typeof(FirebasePhoneVerifyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyPhone(
        [FromBody] FirebasePhoneVerifyRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _firebasePhoneAuthService.VerifyPhoneAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(
                title: "Phone verification failed",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>
    /// Registers a new user after Firebase phone OTP verification.
    /// The user chooses a username and password during signup.
    /// </summary>
    /// <param name="request">The registration request with Firebase token, username, and password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Auth response with access token and user info.</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(
        [FromBody] FirebasePhoneRegisterRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _firebasePhoneAuthService.RegisterWithPhoneAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(
                title: "Registration failed",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>
    /// Authenticates an existing user using Firebase ID token (OTP verification).
    /// No password required - OTP verification is sufficient.
    /// </summary>
    /// <param name="request">The Firebase token verification request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Auth response with access token and user info.</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login(
        [FromBody] FirebasePhoneVerifyRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _firebasePhoneAuthService.LoginWithPhoneAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(
                title: "Login failed",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>
    /// Resets user password after Firebase phone OTP verification.
    /// User must verify their phone number via Firebase OTP before resetting password.
    /// </summary>
    /// <param name="request">The password reset request with Firebase token and new password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Auth response with new access token and user info.</returns>
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] FirebasePasswordResetRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _firebasePhoneAuthService.ResetPasswordAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(
                title: "Password reset failed",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
