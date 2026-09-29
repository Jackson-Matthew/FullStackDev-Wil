using BlastPro.Mvc.Models.Dtos;
using BlastPro.Mvc.Models.ViewModels.Projects;
using BlastPro.Mvc.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlastPro.Mvc.Controllers;

[Authorize]
public sealed class PatternDesignController : Controller
{
    private const string PatternView = "~/Views/Projects/Index.cshtml";
    private readonly IApiClient _api;

    public PatternDesignController(IApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? projectId)
    {
        if (projectId is null)
        {
            var projects = await _api.GetAsync<List<ProjectSummaryDto>>("api/projects");
            if (projects.Success && projects.Data is { Count: > 0 })
                return RedirectToAction(nameof(Index), new { projectId = projects.Data[0].Id });
            if (projects.Success)
                return RedirectToAction("Create", "Projects");
            TempData["Error"] = projects.Error ?? "Could not load projects.";
            return RedirectToAction("Index", "Dashboard");
        }

        var result = await _api.GetAsync<PatternDesignViewModel>(
            $"api/projects/{projectId.Value}/pattern-design");

        if (!result.Success || result.Data is null)
        {
            TempData["Error"] = result.Error ?? "Could not load the pattern design.";
            return RedirectToAction("Index", "Dashboard");
        }

        if (TempData["Success"] is string success) ViewData["Success"] = success;
        return View(PatternView, result.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveDraft(PatternDesignViewModel model, bool continueToResults = false)
    {
        if (!ModelState.IsValid)
        {
            await RestoreExplosiveProducts(model);
            return View(PatternView, model);
        }

        if (model.ProjectId <= 0)
        {
            ModelState.AddModelError(string.Empty, "Create a project before saving this pattern design.");
            return View(PatternView, model);
        }

        var result = await _api.PutAsync<PatternDesignViewModel>(
            $"api/projects/{model.ProjectId}/pattern-design",
            new
            {
                model.RockType,
                model.RockDensity,
                model.Burden,
                model.Spacing,
                model.BenchLengthMetres,
                model.BenchWidthMetres,
                model.LayoutRows,
                model.LayoutColumns,
                model.TimingOrder,
                model.TimingIntervalMilliseconds,
                model.PatternType,
                model.ReferenceExplosiveFamily,
                model.DefaultAeciProductCode,
                model.LoadingDensityGramsPerCc,
                model.Calculation,
                model.VibrationThreshold,
                model.ReceptorStructureType,
                model.DominantFrequencyHz,
                model.VibrationThresholdMode,
                model.RowVersion,
                model.Holes
            });

        if (!result.Success || result.Data is null)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not save the pattern design.");
            await RestoreExplosiveProducts(model);
            return View(PatternView, model);
        }

        TempData["Success"] = "Pattern draft saved.";
        if (continueToResults)
            return RedirectToAction("Index", "Results", new { projectId = model.ProjectId });
        return RedirectToAction(nameof(Index), new { projectId = model.ProjectId });
    }

    private async Task RestoreExplosiveProducts(PatternDesignViewModel model)
    {
        var current = await _api.GetAsync<PatternDesignViewModel>(
            $"api/projects/{model.ProjectId}/pattern-design");
        if (current.Success && current.Data is not null)
        {
            model.ExplosiveProducts = current.Data.ExplosiveProducts;
            model.AeciProducts = current.Data.AeciProducts;
        }
    }

}
