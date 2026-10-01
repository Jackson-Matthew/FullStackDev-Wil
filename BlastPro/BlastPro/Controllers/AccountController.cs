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
    public AccountController(IApiClient api) => _api = api;

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
                ? "Invalid login attempt. Check your email and password."
                : result.Error ?? "The server did not return a sign-in session.");
            return View(model);
        }

        if (!await SignInUserAsync(result.Data, model.RememberMe))
        {
            ModelState.AddModelError(string.Empty, "The server returned an invalid sign-in session.");
            return View(model);
        }
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
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not request a password reset.");
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

    [HttpGet, AllowAnonymous]
    public IActionResult AcceptInvitation(string? email, string? token)
    {
        Response.Headers["Referrer-Policy"] = "no-referrer";
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token) || token.Length > 4096)
            return View("InvitationInvalid");
        return View(new ResetPasswordViewModel { Email = email, Token = token });
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptInvitation(ResetPasswordViewModel model)
    {
        Response.Headers["Referrer-Policy"] = "no-referrer";
        if (!ModelState.IsValid) return View(model);
        var result = await _api.PostAnonymousAsync<object>("api/auth/accept-invitation", new { model.Email, model.Token, model.Password });
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Account setup could not be completed.");
            return View(model);
        }
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["AccountNotice"] = "Account setup complete. Sign in with your new password.";
        return RedirectToAction(nameof(Login));
    }

    // GET: /Account/CreateCompany
    [HttpGet, AllowAnonymous]
    public IActionResult CreateCompany() => User.Identity?.IsAuthenticated == true
        ? RedirectToAction("Index", "Dashboard") : View(new CreateCompanyViewModel());

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCompany(CreateCompanyViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(HttpContext.RequestAborted);
        timeout.CancelAfter(TimeSpan.FromSeconds(12));
        var result = await _api.PostAnonymousAsync<LoginResultDto>("api/companies", new
        {
            model.CompanyName, model.RegistrationNumber, model.ContactEmail, model.ContactPhone,
            model.Address, model.FullName, model.Email, model.Password
        }, timeout.Token);
        if (!result.Success)
        {
            if (result.ValidationErrors is { Count: > 0 })
            {
                foreach (var (field, messages) in result.ValidationErrors)
                {
                    var modelField = ModelState.ContainsKey(field) ? field : string.Empty;
                    if (messages.Length > 0)
                        ModelState.AddModelError(modelField, string.Join(" ", messages));
                }
            }
            else ModelState.AddModelError(string.Empty, result.Error ?? "Company setup could not be completed.");
            return View(model);
        }
        if (result.Data is null || !await SignInUserAsync(result.Data, rememberMe: false))
        {
            TempData["AccountNotice"] = "Company created. Please sign in.";
            return RedirectToAction(nameof(Login));
        }
        return RedirectToAction("Index", "Dashboard");
    }

    // Saved links from the retired confirmation flow now return to ordinary sign in.
    [HttpGet, AllowAnonymous]
    public IActionResult ConfirmationRequested() => RedirectToAction(nameof(Login));

    [HttpGet, AllowAnonymous]
    public IActionResult ConfirmEmail() => RedirectToAction(nameof(Login));

    [HttpGet, AllowAnonymous]
    public IActionResult ResendConfirmation() => RedirectToAction(nameof(Login));

    [HttpGet]
    public IActionResult AccessDenied() => View();

    private async Task<bool> SignInUserAsync(LoginResultDto data, bool rememberMe)
    {
        var now = DateTimeOffset.UtcNow;
        var tokenExpiry = new DateTimeOffset(DateTime.SpecifyKind(data.ExpiresAtUtc, DateTimeKind.Utc));
        if (string.IsNullOrEmpty(data.Token) || tokenExpiry <= now) return false;
        var cookieExpiry = rememberMe ? tokenExpiry : new[] { tokenExpiry, now.AddMinutes(60) }.Min();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, data.UserId),
            new(ClaimTypes.Email, data.Email),
            new(ClaimTypes.Name, data.FullName),
            new("companyId", data.CompanyId.ToString()),
            new("jwt", data.Token)
        };
        foreach (var role in data.Roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity), new AuthenticationProperties
            {
                IsPersistent = rememberMe, IssuedUtc = now,
                ExpiresUtc = cookieExpiry, AllowRefresh = false
            });
        return true;
    }

    private IActionResult RedirectToLocal(string? returnUrl)
        => (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            ? Redirect(returnUrl)
            : RedirectToAction("Index", "Dashboard");
}
