using BlastPro.Api.Models.Dtos;
using BlastPro.Api.Models.Entities;
using BlastPro.Api.Services.Interfaces;
using BlastPro.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using BlastPro.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace BlastPro.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _jwt;
    private readonly ILogger<AuthController> _logger;
    private readonly IPasswordResetDelivery _resetDelivery;
    private readonly IAccountEmailDelivery _accountDelivery;
    private readonly ApplicationDbContext _db;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        IJwtTokenService jwt,
        ILogger<AuthController> logger,
        IPasswordResetDelivery resetDelivery,
        IAccountEmailDelivery accountDelivery,
        ApplicationDbContext db)
    {
        _userManager = userManager;
        _jwt = jwt;
        _logger = logger;
        _resetDelivery = resetDelivery;
        _accountDelivery = accountDelivery;
        _db = db;
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
        if (user is null || !user.IsActive || !user.EmailConfirmed || await _userManager.IsLockedOutAsync(user)
            || !await _db.Companies.AnyAsync(c => c.Id == user.CompanyId && c.IsActive))
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

        if (user is not null && user.IsActive && user.EmailConfirmed)
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
        if (user is null || !user.IsActive || !user.EmailConfirmed || token is null)
            return BadRequest(new { message = invalidLink });

        var result = await _userManager.ResetPasswordAsync(
            user, token, request.NewPassword);

        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e =>
                e.Code == "InvalidToken" ? invalidLink : e.Description) });

        return Ok(new { message = "Password reset complete." });
    }

    [HttpPost("confirm-email"), AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(EmailTokenRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        var token = PasswordResetTokens.Decode(request.Token);
        if (user is null || !user.IsActive || user.EmailConfirmed || token is null
            || !await _userManager.IsInRoleAsync(user, DatabaseSeeder.MainCompanyUserRole))
            return BadRequest(new { message = "This confirmation link is invalid or has expired. Request a new link." });
        var result = await _userManager.ConfirmEmailAsync(user, token);
        return result.Succeeded ? Ok(new { message = "Email confirmed. You can now sign in." })
            : BadRequest(new { message = "This confirmation link is invalid or has expired. Request a new link." });
    }

    [HttpPost("resend-confirmation"), AllowAnonymous]
    public async Task<IActionResult> ResendConfirmation(ForgotPasswordRequest request)
    {
        try
        {
            await _accountDelivery.PrepareAsync();
            var user = await _userManager.FindByEmailAsync(request.Email.Trim());
            if (user is not null && user.IsActive && !user.EmailConfirmed
                && await _userManager.IsInRoleAsync(user, DatabaseSeeder.MainCompanyUserRole))
                await _accountDelivery.SendConfirmationAsync(user.Email!,
                    PasswordResetTokens.Encode(await _userManager.GenerateEmailConfirmationTokenAsync(user)));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            _logger.LogWarning("Confirmation delivery is unavailable.");
            return StatusCode(503, new { message = "Confirmation delivery is temporarily unavailable." });
        }
        return Ok(new { message = "If an eligible account exists, a confirmation link has been requested.", isDevelopmentDelivery = true });
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
