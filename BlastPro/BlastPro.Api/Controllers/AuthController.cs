using BlastPro.Api.Models.Dtos;
using BlastPro.Api.Models.Entities;
using BlastPro.Api.Services.Interfaces;
using BlastPro.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BlastPro.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _jwt;
    private readonly ILogger<AuthController> _logger;
    private readonly IPasswordResetDelivery _resetDelivery;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        IJwtTokenService jwt,
        ILogger<AuthController> logger,
        IPasswordResetDelivery resetDelivery)
    {
        _userManager = userManager;
        _jwt = jwt;
        _logger = logger;
        _resetDelivery = resetDelivery;
    }

    // POST /api/auth/login
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Email and password are required." });

        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || !user.IsActive || await _userManager.IsLockedOutAsync(user))
            return Unauthorized(new { message = "Invalid login attempt." });

        var valid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!valid)
        {
            var failure = await _userManager.AccessFailedAsync(user);
            if (!failure.Succeeded) return StatusCode(503, new { message = "Sign in is temporarily unavailable." });
            return Unauthorized(new { message = "Invalid login attempt." });
        }

        var reset = await _userManager.ResetAccessFailedCountAsync(user);
        if (!reset.Succeeded) return StatusCode(503, new { message = "Sign in is temporarily unavailable." });

        var roles = await _userManager.GetRolesAsync(user);
        var (token, expires) = _jwt.CreateToken(user, roles);

        return Ok(new LoginResponse
        {
            Token = token,
            ExpiresAtUtc = expires,
            UserId = user.Id,
            Email = user.Email!,
            FullName = user.FullName,
            CompanyId = user.CompanyId,
            Roles = roles
        });
    }

    // POST /api/auth/forgot-password
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        // Delivery health is checked before the address to avoid account discovery.
        try { await _resetDelivery.PrepareAsync(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            _logger.LogWarning("Password reset delivery is unavailable.");
            return StatusCode(503, new { message = "Password reset is temporarily unavailable." });
        }
        var user = await _userManager.FindByEmailAsync(request.Email.Trim());

        if (user is not null && user.IsActive)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            try { await _resetDelivery.SendAsync(user.Email!, PasswordResetTokens.Encode(token)); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                _logger.LogError("A password reset message could not be saved.");
            }
        }

        // Always same response — do not leak whether the account exists
        return Ok(new { message = "If an eligible account exists, a reset link has been requested.", isDevelopmentDelivery = true });
    }

    // POST /api/auth/reset-password
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        const string invalidLink = "This reset link is invalid or has expired. Please request a new link.";
        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        var token = PasswordResetTokens.Decode(request.Token);
        if (user is null || !user.IsActive || token is null)
            return BadRequest(new { message = invalidLink });

        var result = await _userManager.ResetPasswordAsync(
            user, token, request.NewPassword);

        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e =>
                e.Code == "InvalidToken" ? invalidLink : e.Description) });

        return Ok(new { message = "Password reset complete." });
    }

    // GET /api/auth/me
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new
        {
            user.Id,
            user.Email,
            user.FullName,
            user.CompanyId,
            user.IsActive,
            Roles = roles
        });
    }
}
