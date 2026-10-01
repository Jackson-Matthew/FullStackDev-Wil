using System.Security.Claims;
using BlastPro.Mvc.Models.ViewModels;
using BlastPro.Mvc.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlastPro.Mvc.Controllers;

[Authorize]
public sealed class ProfileController(IApiClient api) : Controller
{
    [HttpGet]
    public Task<IActionResult> Index() => Load(nameof(Index));

    [HttpGet]
    public Task<IActionResult> Edit() => Load(nameof(Edit));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProfileViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await api.PutAsync<object>("api/profile", new
            { model.FullName, model.PhoneNumber, model.NickName, model.Gender, model.Country, model.TimeZoneId });
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not save your profile.");
            return View(model);
        }
        // Refresh the displayed name while retaining the original token and cookie expiry.
        var session = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (session.Principal?.Identity is ClaimsIdentity identity)
        {
            var name = identity.FindFirst(ClaimTypes.Name);
            if (name is not null) identity.RemoveClaim(name);
            identity.AddClaim(new Claim(ClaimTypes.Name, model.FullName.Trim()));
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, session.Principal, session.Properties);
        }
        TempData["Success"] = "Profile saved.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> Load(string view)
    {
        var result = await api.GetAsync<ProfileViewModel>("api/profile");
        if (result.Success && result.Data is not null) return View(view, result.Data);
        TempData["Error"] = result.Error ?? "Could not load your profile.";
        return RedirectToAction("Index", "Dashboard");
    }
}
