using BlastPro.Api.Data;
using BlastPro.Api.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlastPro.Api.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ProjectsController(ApplicationDbContext db) => _db = db;

    // GET /api/projects
    [HttpGet]
    public async Task<IActionResult> GetMyProjects()
    {
        var userId = User.GetUserId();
        var companyId = User.GetCompanyId();
        if (userId is null || companyId is null) return Unauthorized();

        var isMainCompanyUser = User.IsInRole("MainCompanyUser");

        var query = _db.BlastProjects
            .Where(p => p.CompanyId == companyId);

        // Blasters only see their own; MainCompanyUser sees all company projects
        if (!isMainCompanyUser)
            query = query.Where(p => p.OwnerId == userId);

        var results = await query
            .OrderByDescending(p => p.UpdatedAtUtc)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.SiteLocation,
                p.BlastType,
                Status = p.Status.ToString(),
                p.CreatedAtUtc,
                p.UpdatedAtUtc
            })
            .ToListAsync();

        return Ok(results);
    }

    // GET /api/projects/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetProject(int id)
    {
        var companyId = User.GetCompanyId();
        if (companyId is null) return Unauthorized();

        var project = await _db.BlastProjects
            .Where(p => p.Id == id && p.CompanyId == companyId)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.SiteLocation,
                p.BlastType,
                p.RockType,
                p.RockDensity,
                p.Burden,
                p.Spacing,
                p.VibrationThreshold,
                Status = p.Status.ToString(),
                p.CreatedAtUtc,
                p.UpdatedAtUtc
            })
            .FirstOrDefaultAsync();

        if (project is null) return NotFound();
        return Ok(project);
    }
}