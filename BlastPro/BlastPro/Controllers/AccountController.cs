using System.Security.Claims;
using BlastPro.Mvc.Models.ViewModels.Account;
using BlastPro.Mvc.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlastPro.Mvc.Controllers;

[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AccountController : Controller
{
    private readonly IApiClient _api;
    private readonly ILogger<AccountController> _logger;

    public AccountController(IApiClient api, ILogger<AccountController> logger)
    {
        _api = api;
        _logger = logger;
    }

    // GET: /Account/Login
    [HttpGet]
    [AllowAnonymous]                 
    public IActionResult Login(string? returnUrl = null, bool expired = false)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");

        if (expired) ViewData["Notice"] = "Your session has ended. Please sign in again.";
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    // POST: /Account/Login
    [HttpPost]
    [AllowAnonymous]                  
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _api.LoginAsync(model.Email, model.Password);
        if (!result.Success || result.Data is null)
        {
            ModelState.AddModelError(string.Empty, result.StatusCode == System.Net.HttpStatusCode.Unauthorized
                ? "Invalid login attempt. Check your details or try again later."
                : "Sign in is temporarily unavailable. Please try again shortly.");
            return View(model);
        }

        var now = DateTimeOffset.UtcNow;
        var tokenExpiry = new DateTimeOffset(DateTime.SpecifyKind(result.Data.ExpiresAtUtc, DateTimeKind.Utc));
        if (string.IsNullOrEmpty(result.Data.Token) || tokenExpiry <= now)
        {
            ModelState.AddModelError(string.Empty, "Sign in is temporarily unavailable. Please try again shortly.");
            return View(model);
        }
        var cookieExpiry = model.RememberMe ? tokenExpiry : new[] { tokenExpiry, now.AddMinutes(60) }.Min();
        var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, result.Data.UserId),
        new(ClaimTypes.Email,          result.Data.Email),
        new(ClaimTypes.Name,           result.Data.FullName),
        new("companyId",               result.Data.CompanyId.ToString()),
        new("jwt",                     result.Data.Token)
    };
        foreach (var role in result.Data.Roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = model.RememberMe, IssuedUtc = now,
                ExpiresUtc = cookieExpiry, AllowRefresh = false
            });

        return RedirectToLocal(model.ReturnUrl);
    }

    // POST: /Account/Logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    // GET: /Account/ForgotPassword
    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    // POST: /Account/ForgotPassword
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _api.ForgotPasswordAsync(model.Email);
        if (!result.Success || result.Data is null)
        {
            ModelState.AddModelError(string.Empty, "Password reset is temporarily unavailable. Please try again later.");
            return View(model);
        }
        TempData["DevelopmentResetDelivery"] = result.Data.IsDevelopmentDelivery;

        // Always show the same confirmation — do not reveal account existence
        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    // GET: /Account/ForgotPasswordConfirmation
    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPasswordConfirmation()
    {
        if (TempData["DevelopmentResetDelivery"] is not bool developmentDelivery)
            return RedirectToAction(nameof(ForgotPassword));
        ViewData["DevelopmentResetDelivery"] = developmentDelivery;
        return View();
    }

    // GET: /Account/ResetPassword
    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPassword(string? email, string? token)
    {
        Response.Headers["Referrer-Policy"] = "no-referrer";
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token) || token.Length > 4096)
            return View("ResetPasswordInvalid");

        return View(new ResetPasswordViewModel { Email = email, Token = token });
    }

    // POST: /Account/ResetPassword
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        Response.Headers["Referrer-Policy"] = "no-referrer";
        if (!ModelState.IsValid) return View(model);

        var result = await _api.ResetPasswordAsync(
            model.Email, model.Token, model.Password);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty,
                result.Error ?? "Password reset failed.");
            return View(model);
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["PasswordResetComplete"] = true;
        return RedirectToAction(nameof(ResetPasswordConfirmation));
    }

    // GET: /Account/ResetPasswordConfirmation
    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPasswordConfirmation()
        => TempData["PasswordResetComplete"] is true ? View() : RedirectToAction(nameof(Login));

    // GET: /Account/AccessDenied
    [HttpGet]
    public IActionResult AccessDenied() => View();

    private IActionResult RedirectToLocal(string? returnUrl)
        => (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            ? Redirect(returnUrl)
            : RedirectToAction("Index", "Dashboard");
}
