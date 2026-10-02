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
    // Fields that have a message slot in the view; anything else is shown in the summary.
    private static readonly HashSet<string> FieldNames = new(StringComparer.Ordinal)
    {
        "RockType", "RockDensity", "Burden", "Spacing", "BenchLengthMetres", "BenchWidthMetres",
        "LayoutRows", "LayoutColumns", "TimingOrder", "TimingIntervalMilliseconds", "PatternType",
        "DefaultAeciProductCode", "VibrationThreshold", "ReceptorStructureType", "DominantFrequencyHz",
        "Calculation.DelayWindowMilliseconds",
        "Calculation.ReceptorDistanceMetres", "Calculation.PpvSiteCoefficient",
        "Calculation.PpvDecayExponent", "Calculation.FlyrockLaunchSpeedMetresPerSecond",
        "Calculation.FlyrockLaunchAngleDegrees", "Calculation.FlyrockLaunchHeightMetres",
        "Calculation.ExclusionRadiusMetres"
    };

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
            SummariseHoleErrors();
            await RestoreExplosiveProducts(model);
            return View(PatternView, model);
        }

        if (model.ProjectId <= 0)
        {
            ModelState.AddModelError(string.Empty, "Create a project before saving this pattern design.");
            return View(PatternView, model);
        }

        // An unchosen product dropdown posts "" which binds as null; the API expects text.
        foreach (var hole in model.Holes) hole.AeciProductCode ??= "";

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
                ReferenceExplosiveFamily = model.ReferenceExplosiveFamily ?? "",
                DefaultAeciProductCode = model.DefaultAeciProductCode ?? "",
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
            if (result.ValidationErrors is { Count: > 0 })
            {
                foreach (var (field, messages) in result.ValidationErrors)
                {
                    var modelField = FieldNames.Contains(field) ? field : string.Empty;
                    foreach (var message in messages)
                        ModelState.AddModelError(modelField, message);
                }
            }
            else ModelState.AddModelError(string.Empty, result.Error ?? "Could not save the pattern design.");
            await RestoreExplosiveProducts(model);
            return View(PatternView, model);
        }

        TempData["Success"] = "Pattern draft saved.";
        if (continueToResults)
            return RedirectToAction("Index", "Results", new { projectId = model.ProjectId });
        return RedirectToAction(nameof(Index), new { projectId = model.ProjectId });
    }

    // Hole cells have no message slot of their own, so their errors go in the summary.
    private void SummariseHoleErrors()
    {
        var holeErrors = ModelState
            .Where(entry => entry.Key.StartsWith("Holes[", StringComparison.Ordinal))
            .SelectMany(entry => entry.Value!.Errors.Select(error =>
                $"Hole {HoleNumber(entry.Key)}: {error.ErrorMessage}"))
            .Distinct()
            .ToList();
        foreach (var message in holeErrors)
            ModelState.AddModelError(string.Empty, message);
    }

    private static string HoleNumber(string key)
    {
        var end = key.IndexOf(']');
        return int.TryParse(key.AsSpan(6, Math.Max(0, end - 6)), out var index) ? (index + 1).ToString() : "?";
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
