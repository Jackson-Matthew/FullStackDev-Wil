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
        return View(new BlasterViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BlasterViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await api.PostAsync<object>("api/company/blasters", new
                { model.FullName, model.Email, model.PhoneNumber, model.CertificationId });
            if (result.Success)
            {
                TempData["Success"] = "Blaster added. Their invitation is in the local development mailbox; they choose their own password.";
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

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(string id, bool isActive)
    {
        var result = await api.PutAsync<object>($"api/company/blasters/{Uri.EscapeDataString(id)}/status", new { isActive });
        if (result.StatusCode == System.Net.HttpStatusCode.NotFound) return NotFound();
        TempData[result.Success ? "Success" : "Error"] = result.Success
            ? (isActive ? "Blaster reactivated." : "Blaster deactivated. Their designs are retained.") : result.Error;
        return RedirectToAction("Index", "Company");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendInvitation(string id)
    {
        var result = await api.PostAsync<object>($"api/company/blasters/{Uri.EscapeDataString(id)}/invitation", new { });
        if (result.StatusCode == System.Net.HttpStatusCode.NotFound) return NotFound();
        TempData[result.Success ? "Success" : "Error"] = result.Success
            ? "New invitation requested. Open the latest message in the local development mailbox." : result.Error;
        return RedirectToAction("Index", "Company");
    }

    private async Task LoadSeatCount()
    {
        var result = await api.GetAsync<CompanyViewModel>("api/company");
        if (result.Success && result.Data is not null)
            ViewData["Seats"] = $"{result.Data.ActiveBlasterCount} of {result.Data.BlasterLimit} active Blasters";
    }
}
