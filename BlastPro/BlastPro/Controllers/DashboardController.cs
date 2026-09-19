using BlastPro.Mvc.Models.Dtos;
using BlastPro.Mvc.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlastPro.Mvc.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IApiClient _api;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IApiClient api, ILogger<DashboardController> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var result = await _api.GetAsync<List<ProjectSummaryDto>>("api/projects");

        if (!result.Success)
        {
            _logger.LogWarning("Dashboard API call failed: {Error}", result.Error);
            ViewData["Error"] = "Could not load projects. The API may be offline.";
            return View(new List<ProjectSummaryDto>());
        }

        return View(result.Data ?? new List<ProjectSummaryDto>());
    }
}