using BlastPro.Mvc.Models.Dtos;
using BlastPro.Mvc.Models.ViewModels.Results;
using BlastPro.Mvc.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlastPro.Mvc.Controllers;

[Authorize]
public sealed class ResultsController(IApiClient api, ILogger<ResultsController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int projectId, int? resultId)
    {
        if (projectId <= 0)
            return View(new ResultsViewModel());

        var response = await api.GetAsync<ProjectResultsDto>($"api/projects/{projectId}/results");
        if (!response.Success || response.Data is null)
        {
            logger.LogWarning("Could not load results for project {ProjectId}: {Error}", projectId, response.Error);
            TempData["Error"] = "The project results could not be opened.";
            return RedirectToAction("Index", "Dashboard");
        }

        var page = response.Data;
        var selected = page.Result;
        if (resultId.HasValue)
        {
            var historical = await api.GetAsync<CalculationResultDto>(
                $"api/projects/{projectId}/results/{resultId.Value}");
            if (!historical.Success || historical.Data is null)
            {
                logger.LogWarning("Could not load result {ResultId} for project {ProjectId}: {Error}",
                    resultId.Value, projectId, historical.Error);
                TempData["Error"] = "The saved result could not be opened.";
                return RedirectToAction("Index", "Dashboard");
            }
            selected = historical.Data;
        }

        var model = new ResultsViewModel
        {
            ProjectId = page.ProjectId,
            ProjectName = page.ProjectName,
            HasResults = page.HasResults,
            ResultId = selected?.Id,
            ViewingHistory = resultId.HasValue && selected?.Id != page.Result?.Id,
            IsOutdated = selected is not null && !selected.IsCurrent,
            CalculatedAtUtc = selected?.CalculatedAtUtc,
            CalculatedByName = selected?.CalculatedByName,
            TotalHoles = selected?.TotalHoles ?? 0,
            TotalExplosiveKg = selected?.TotalExplosiveKg ?? 0,
            TotalDrillingMetres = selected?.TotalDrillingMetres ?? 0,
            TotalCost = selected?.TotalCost,
            CurrencyCode = selected?.CurrencyCode,
            MaxChargePerDelayKg = selected?.MaxChargePerDelayKg ?? 0,
            PowderFactorKgPerTonne = selected?.PowderFactorKgPerTonne,
            EstimatedVolumeCubicMetres = selected?.EstimatedVolumeCubicMetres,
            EstimatedTonnageTonnes = selected?.EstimatedTonnageTonnes,
            PredictedPpvMmPerSecond = selected?.PredictedPpvMmPerSecond,
            PredictedFlyrockMetres = selected?.PredictedFlyrockMetres,
            Warnings = selected?.Warnings.Select(w => new ResultWarningViewModel
            {
                Code = w.Code,
                Severity = w.Severity,
                Message = w.Message
            }).ToList() ?? new()
        };

        var history = page.History.ToList();
        if (model.ViewingHistory && page.Result is { } latest)
            history.Add(new CalculationHistoryDto
            {
                Id = latest.Id,
                CalculatedAtUtc = latest.CalculatedAtUtc,
                CalculatedByName = latest.CalculatedByName,
                TotalCost = latest.TotalCost,
                CurrencyCode = latest.CurrencyCode,
                PowderFactorKgPerTonne = latest.PowderFactorKgPerTonne,
                WarningCount = latest.Warnings.Count
            });
        model.PreviousResults = history.Where(r => r.Id != selected?.Id)
            .OrderByDescending(r => r.CalculatedAtUtc)
            .ThenByDescending(r => r.Id)
            .Select(r => new PreviousResultViewModel
            {
                Id = r.Id,
                CalculatedAtUtc = r.CalculatedAtUtc,
                CalculatedByName = r.CalculatedByName,
                TotalCost = r.TotalCost,
                CurrencyCode = r.CurrencyCode,
                PowderFactorKgPerTonne = r.PowderFactorKgPerTonne,
                WarningCount = r.WarningCount
            }).ToList();

        if (TempData["Success"] is string success)
            ViewData["Success"] = success;
        return View(model);
    }
}
