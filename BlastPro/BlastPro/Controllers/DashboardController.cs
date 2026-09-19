using BlastPro.Mvc.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlastPro.Mvc.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IApiClient _api;

    public DashboardController(IApiClient api) => _api = api;

    public async Task<IActionResult> Index()
    {
        var result = await _api.GetAsync<List<ProjectSummaryDto>>("api/projects");

        if (!result.Success)
        {
            // If the API returns 401, log the user out of the MVC cookie
            return RedirectToAction("Login", "Account");
        }

        return View(result.Data ?? new List<ProjectSummaryDto>());
    }
}

public class ProjectSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SiteLocation { get; set; } = string.Empty;
    public string BlastType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}