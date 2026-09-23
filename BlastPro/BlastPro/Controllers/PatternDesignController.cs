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

            var blank = CreateBlankDesign();
            ViewData[projects.Success ? "Info" : "Error"] = projects.Success
                ? "No saved project is available yet. You can test the layout, but saving requires a project."
                : projects.Error ?? "Could not load projects.";
            return View(PatternView, blank);
        }

        var result = await _api.GetAsync<PatternDesignViewModel>(
            $"api/projects/{projectId.Value}/pattern-design");

        if (!result.Success || result.Data is null)
        {
            ViewData["Error"] = result.Error ?? "Could not load the pattern design.";
            return View(PatternView, CreateBlankDesign());
        }

        return View(PatternView, result.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveDraft(PatternDesignViewModel model)
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
                model.VibrationThreshold,
                model.RowVersion,
                model.Holes
            });

        if (!result.Success || result.Data is null)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not save the pattern design.");
            await RestoreExplosiveProducts(model);
            return View(PatternView, model);
        }

        ViewData["Success"] = "Draft layout saved.";
        return View(PatternView, result.Data);
    }

    private async Task RestoreExplosiveProducts(PatternDesignViewModel model)
    {
        var current = await _api.GetAsync<PatternDesignViewModel>(
            $"api/projects/{model.ProjectId}/pattern-design");
        if (current.Success && current.Data is not null)
            model.ExplosiveProducts = current.Data.ExplosiveProducts;
    }

    private static PatternDesignViewModel CreateBlankDesign()
    {
        return new PatternDesignViewModel
        {
            ProjectName = "Pattern Design",
            Status = "Draft",
            RockType = "Granite"
        };
    }
}
