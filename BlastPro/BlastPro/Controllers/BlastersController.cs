using BlastPro.Mvc.Models.ViewModels;
using BlastPro.Mvc.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlastPro.Mvc.Controllers;

[Authorize(Roles = "MainCompanyUser")]
public sealed class BlastersController(IApiClient api) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadSeatCount();
        return View(new CreateBlasterViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateBlasterViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await api.PostAsync<object>("api/company/blasters", new
                { model.Email, model.Password });
            if (result.Success)
            {
                TempData["Success"] = "Blaster account created. Share the sign-in details securely.";
                return RedirectToAction("Index", "Company");
            }
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not add the Blaster.");
        }
        await LoadSeatCount();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var result = await api.GetAsync<BlasterViewModel>($"api/company/blasters/{Uri.EscapeDataString(id)}");
        if (result.StatusCode == System.Net.HttpStatusCode.NotFound) return NotFound();
        if (result.Success && result.Data is not null) return View(result.Data);
        TempData["Error"] = result.Error;
        return RedirectToAction("Index", "Company");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, BlasterViewModel model)
    {
        // Email is displayed as read-only and is deliberately excluded from the update payload.
        ModelState.Remove(nameof(model.Email));
        if (ModelState.IsValid)
        {
            var result = await api.PutAsync<object>($"api/company/blasters/{Uri.EscapeDataString(id)}", new
                { model.FullName, model.PhoneNumber, model.CertificationId });
            if (result.StatusCode == System.Net.HttpStatusCode.NotFound) return NotFound();
            if (result.Success)
            {
                TempData["Success"] = "Blaster details saved.";
                return RedirectToAction("Index", "Company");
            }
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not save Blaster details.");
        }
        model.Id = id;
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> SetPassword(string id)
    {
        var result = await api.GetAsync<BlasterViewModel>($"api/company/blasters/{Uri.EscapeDataString(id)}");
        if (result.StatusCode == System.Net.HttpStatusCode.NotFound) return NotFound();
        if (result.Success && result.Data is not null)
            return View(new BlasterPasswordViewModel { Id = id, Email = result.Data.Email });
        TempData["Error"] = result.Error;
        return RedirectToAction("Index", "Company");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPassword(string id, BlasterPasswordViewModel model)
    {
        model.Id = id;
        if (!ModelState.IsValid) return View(model);
        var result = await api.PutAsync<object>($"api/company/blasters/{Uri.EscapeDataString(id)}/password",
            new { model.Password });
        if (result.StatusCode == System.Net.HttpStatusCode.NotFound) return NotFound();
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not set the password.");
            return View(model);
        }
        TempData["Success"] = "Blaster password set. Share it securely; any previous sign-in has been ended.";
        return RedirectToAction("Index", "Company");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(string id, bool isActive)
    {
        var result = await api.PutAsync<object>($"api/company/blasters/{Uri.EscapeDataString(id)}/status", new { isActive });
        if (result.StatusCode == System.Net.HttpStatusCode.NotFound) return NotFound();
        TempData[result.Success ? "Success" : "Error"] = result.Success
            ? (isActive ? "Blaster reactivated." : "Blaster deactivated. Their designs are retained.") : result.Error;
        return RedirectToAction("Index", "Company");
    }

    private async Task LoadSeatCount()
    {
        var result = await api.GetAsync<CompanyViewModel>("api/company");
        if (result.Success && result.Data is not null)
            ViewData["Seats"] = $"{result.Data.ActiveBlasterCount} of {result.Data.BlasterLimit} active Blasters";
    }
}
