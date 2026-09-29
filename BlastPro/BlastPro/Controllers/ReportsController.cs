using BlastPro.Mvc.Models.Dtos;
using BlastPro.Mvc.Models.ViewModels.Projects;
using BlastPro.Mvc.Models.ViewModels.Reports;
using BlastPro.Mvc.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlastPro.Mvc.Controllers;

[Authorize]
public sealed class ReportsController(IApiClient api) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var projects = await api.GetAsync<List<ProjectSummaryDto>>("api/projects");
        if (!projects.Success || projects.Data is null)
        {
            TempData["Error"] = projects.Error ?? "Could not load reports.";
            return RedirectToAction("Index", "Dashboard");
        }
        return View(projects.Data);
    }

    [HttpGet]
    public async Task<IActionResult> Preview(int projectId, int? resultId)
    {
        var report = await LoadReport(projectId, resultId);
        if (report is null) return RedirectToAction("Index", "Dashboard");
        return View(report);
    }

    [HttpGet]
    public async Task<IActionResult> Print(int projectId, int? resultId)
    {
        var report = await LoadReport(projectId, resultId);
        if (report is null) return RedirectToAction("Index", "Dashboard");
        if (!report.HasResults)
            return RedirectToAction(nameof(Preview), new { projectId });
        return View(report);
    }

    [HttpGet]
    public IActionResult DownloadPdf(int projectId, int? resultId) =>
        RedirectToAction(nameof(Print), new { projectId, resultId });

    private async Task<ReportViewModel?> LoadReport(int projectId, int? resultId)
    {
        if (projectId <= 0) return new ReportViewModel();

        var results = await api.GetAsync<ProjectResultsDto>($"api/projects/{projectId}/results");
        if (!results.Success || results.Data is null)
        {
            TempData["Error"] = results.Error ?? "Could not open the project report.";
            return null;
        }

        var selected = results.Data.Result;
        if (resultId.HasValue && resultId != selected?.Id)
        {
            var historical = await api.GetAsync<CalculationResultDto>(
                $"api/projects/{projectId}/results/{resultId.Value}");
            if (!historical.Success || historical.Data is null)
            {
                TempData["Error"] = historical.Error ?? "Could not open the selected result.";
                return null;
            }
            selected = historical.Data;
        }

        var report = new ReportViewModel
        {
            ProjectId = projectId,
            ProjectName = results.Data.ProjectName,
            HasResults = selected is not null
        };
        if (selected is null) return report;

        report.ResultId = selected.Id;
        report.ReportDateUtc = selected.CalculatedAtUtc;
        report.CalculatedByName = selected.CalculatedByName;
        report.IsOutdated = !selected.IsCurrent;
        report.TotalDesignatedHoles = selected.TotalHoles;
        report.TotalExplosiveLoadKg = selected.TotalExplosiveKg;
        report.TotalDrillingLengthMetres = selected.TotalDrillingMetres;
        report.EstimatedVolumeCubicMetres = selected.EstimatedVolumeCubicMetres;
        report.TotalEstimatedTonnageTonnes = selected.EstimatedTonnageTonnes;
        report.PowderFactorKgPerTonne = selected.PowderFactorKgPerTonne;
        report.TotalEstimatedMaterialCost = selected.TotalCost;
        report.CurrencyCode = selected.CurrencyCode;
        report.MaxChargePerDelayKg = selected.MaxChargePerDelayKg;
        report.PredictedPpvMmPerSecond = selected.PredictedPpvMmPerSecond;
        report.IdealizedFlyrockRangeMetres = selected.PredictedFlyrockMetres;
        report.Warnings = selected.Warnings.Select(w => new ReportWarningViewModel
        {
            Severity = w.Severity,
            Message = w.Message
        }).ToList();

        if (selected.PatternSnapshot is { } snapshot)
        {
            report.HasPatternSnapshot = true;
            report.SiteLocation = snapshot.SiteLocation;
            report.BlastType = snapshot.BlastType;
            report.RockType = snapshot.RockType;
            report.BenchLengthMetres = snapshot.BenchLengthMetres;
            report.BenchWidthMetres = snapshot.BenchWidthMetres;
            report.BurdenMetres = snapshot.BurdenMetres;
            report.SpacingMetres = snapshot.SpacingMetres;
            report.VibrationThresholdMmPerSecond = snapshot.VibrationThresholdMmPerSecond;
            report.Holes = snapshot.Holes.OrderBy(hole => hole.Number).Select(hole =>
                new ReportHoleViewModel
                {
                    Number = hole.Number,
                    X = hole.X, Y = hole.Y, Depth = hole.Depth,
                    DiameterMillimetres = hole.DiameterMillimetres,
                    SubdrillMetres = hole.SubdrillMetres,
                    Explosive = hole.ProductName,
                    ProductDensityGramsPerCc = hole.ProductDensityGramsPerCc,
                    Charge = hole.Charge, Stemming = hole.Stemming, Delay = hole.Delay
                }).ToList();
            report.HolesMatchResult = report.Holes.Count == selected.TotalHoles;
            return report;
        }

        var project = await api.GetAsync<ProjectDetailDto>($"api/projects/{projectId}");
        if (project.Success && project.Data is not null)
        {
            if (selected.IsCurrent)
            {
                report.SiteLocation = project.Data.SiteLocation;
                report.BlastType = project.Data.BlastType;
                report.RockType = project.Data.RockType ?? "";
                report.BurdenMetres = project.Data.Burden;
                report.SpacingMetres = project.Data.Spacing;
                report.VibrationThresholdMmPerSecond = project.Data.VibrationThreshold;
            }
        }

        // Current holes cannot be shown as the holes of an earlier calculation.
        if (selected.IsCurrent)
        {
            var design = await api.GetAsync<PatternDesignViewModel>(
                $"api/projects/{projectId}/pattern-design");
            if (design.Success && design.Data is not null)
            {
                report.BenchLengthMetres = design.Data.BenchLengthMetres;
                report.BenchWidthMetres = design.Data.BenchWidthMetres;
                var products = design.Data.ExplosiveProducts.ToDictionary(p => p.Id, p => p.Name);
                var aeciProducts = design.Data.AeciProducts.ToDictionary(p => p.Code, p => p.Name);
                report.Holes = design.Data.Holes.OrderBy(h => h.Number).Select(h =>
                    new ReportHoleViewModel
                    {
                        Number = h.Number,
                        X = h.X,
                        Y = h.Y,
                        Depth = h.Depth,
                        DiameterMillimetres = h.DiameterMillimetres,
                        SubdrillMetres = h.SubdrillMetres,
                        Explosive = !string.IsNullOrEmpty(h.AeciProductCode) &&
                            aeciProducts.TryGetValue(h.AeciProductCode, out var aeciName)
                            ? aeciName
                            : h.ExplosiveProductId is int id && products.TryGetValue(id, out var name)
                                ? name : "Not assigned",
                        ProductDensityGramsPerCc = h.ProductDensityGramsPerCc,
                        Charge = h.Charge,
                        Stemming = h.Stemming,
                        Delay = h.Delay
                    }).ToList();
                report.HolesMatchResult = report.Holes.Count == selected.TotalHoles;
            }
        }

        return report;
    }
}
