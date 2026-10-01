using BlastPro.Mvc.Models.ViewModels;
using BlastPro.Mvc.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlastPro.Mvc.Controllers;

[Authorize(Roles = "MainCompanyUser")]
public sealed class CompanyController(IApiClient api) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var result = await api.GetAsync<CompanyViewModel>("api/company");
        if (result.Success && result.Data is not null) return View(result.Data);
        TempData["Error"] = result.Error ?? "Could not load your company.";
        return RedirectToAction("Index", "Dashboard");
    }

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var result = await api.GetAsync<CompanyViewModel>("api/company");
        if (result.Success && result.Data is not null) return View(result.Data);
        TempData["Error"] = result.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CompanyViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await api.PutAsync<object>("api/company", new { model.ContactEmail, model.ContactPhone, model.Address });
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not save your company details.");
            return View(model);
        }
        TempData["Success"] = "Company contact details saved.";
        return RedirectToAction(nameof(Index));
    }
}
