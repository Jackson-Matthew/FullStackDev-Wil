using BlastPro.Mvc.Models.ViewModels;
using BlastPro.Mvc.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlastPro.Mvc.Controllers;

[Authorize(Roles = "MainCompanyUser")]
public sealed class ProductsController(IApiClient api) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var result = await api.GetAsync<List<ProductViewModel>>("api/company/products");
        if (result.Success) return View(result.Data ?? []);
        TempData["Error"] = result.Error ?? "Could not load products.";
        return RedirectToAction("Index", "Company");
    }

    [HttpGet]
    public IActionResult Create() => View("Form", new ProductFormViewModel());

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await api.GetAsync<List<ProductViewModel>>("api/company/products");
        var product = result.Data?.FirstOrDefault(p => p.Id == id);
        if (product is null) return NotFound();
        return View("Form", new ProductFormViewModel
        {
            Id = product.Id, Name = product.Name, PricePerKg = product.PricePerKg,
            IsActive = product.IsActive
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(ProductFormViewModel model)
    {
        if (!ModelState.IsValid) return View("Form", model);
        var payload = new { model.Name, model.PricePerKg, model.IsActive };
        var result = model.Id == 0
            ? await api.PostAsync<object>("api/company/products", payload)
            : await api.PutAsync<object>($"api/company/products/{model.Id}", payload);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not save the product.");
            return View("Form", model);
        }
        TempData["Success"] = "Product saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSamples()
    {
        var result = await api.PostAsync<List<ProductViewModel>>("api/company/products/samples", new { });
        TempData[result.Success ? "Success" : "Error"] = result.Success
            ? "Sample prices added. Replace these estimates with your supplier prices before using costs for decisions."
            : result.Error ?? "Could not add sample prices.";
        return RedirectToAction(nameof(Index));
    }
}
