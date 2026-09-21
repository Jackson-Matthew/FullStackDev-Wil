using BlastPro.Api.Data;
using BlastPro.Api.Extensions;
using BlastPro.Api.Models.Dtos;
using BlastPro.Api.Models.Entities;
using BlastPro.Api.Models.Enums;
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

    // ---------------------------------------------------------------------
    // GET /api/projects
    // Returns projects visible to the signed-in user.
    //  - Blaster: only their own
    //  - MainCompanyUser: all projects in the company
    // ---------------------------------------------------------------------
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProjectSummaryDto>>> GetMyProjects()
    {
        var userId = User.GetUserId();
        var companyId = User.GetCompanyId();
        if (userId is null || companyId is null) return Unauthorized();

        var isMainCompanyUser = User.IsInRole("MainCompanyUser");

        var query = _db.BlastProjects
            .Where(p => p.CompanyId == companyId);

        if (!isMainCompanyUser)
            query = query.Where(p => p.OwnerId == userId);

        var results = await query
            .OrderByDescending(p => p.UpdatedAtUtc)
            .Select(p => new ProjectSummaryDto
            {
                Id = p.Id,
                Name = p.Name,
                SiteLocation = p.SiteLocation,
                BlastType = p.BlastType,
                Status = p.Status.ToString(),
                CreatedAtUtc = p.CreatedAtUtc,
                UpdatedAtUtc = p.UpdatedAtUtc
            })
            .ToListAsync();

        return Ok(results);
    }

    // ---------------------------------------------------------------------
    // GET /api/projects/{id}
    // ---------------------------------------------------------------------
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProjectDetailDto>> GetProject(int id)
    {
        var userId = User.GetUserId();
        var companyId = User.GetCompanyId();
        if (userId is null || companyId is null) return Unauthorized();

        var project = await _db.BlastProjects
            .Where(p => p.Id == id && p.CompanyId == companyId)
            .Select(p => new ProjectDetailDto
            {
                Id = p.Id,
                Name = p.Name,
                SiteLocation = p.SiteLocation,
                BlastType = p.BlastType,
                RockType = p.RockType,
                RockDensity = p.RockDensity,
                Burden = p.Burden,
                Spacing = p.Spacing,
                VibrationThreshold = p.VibrationThreshold,
                Status = p.Status.ToString(),
                CreatedAtUtc = p.CreatedAtUtc,
                UpdatedAtUtc = p.UpdatedAtUtc
            })
            .FirstOrDefaultAsync();

        if (project is null) return NotFound();

        // Blasters can only see their own; MainCompanyUser sees all in company.
        if (!User.IsInRole("MainCompanyUser"))
        {
            var ownsIt = await _db.BlastProjects
                .AnyAsync(p => p.Id == id && p.OwnerId == userId);

            if (!ownsIt) return NotFound();
        }

        return Ok(project);
    }

    // ---------------------------------------------------------------------
    // POST /api/projects
    // ---------------------------------------------------------------------
    [HttpPost]
    public async Task<ActionResult<ProjectDetailDto>> CreateProject(
        [FromBody] CreateProjectDto dto)
    {
        var userId = User.GetUserId();
        var companyId = User.GetCompanyId();
        if (userId is null || companyId is null) return Unauthorized();

        // Validation
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { message = "Project name is required." });

        if (string.IsNullOrWhiteSpace(dto.SiteLocation))
            return BadRequest(new { message = "Site location is required." });

        if (string.IsNullOrWhiteSpace(dto.BlastType))
            return BadRequest(new { message = "Blast type is required." });

        if (dto.Name.Length > 150)
            return BadRequest(new { message = "Project name cannot exceed 150 characters." });

        if (dto.SiteLocation.Length > 250)
            return BadRequest(new { message = "Site location cannot exceed 250 characters." });

        if (dto.BlastType.Length > 100)
            return BadRequest(new { message = "Blast type cannot exceed 100 characters." });

        var project = new BlastProject
        {
            Name = dto.Name.Trim(),
            SiteLocation = dto.SiteLocation.Trim(),
            BlastType = dto.BlastType.Trim(),
            CompanyId = companyId.Value,
            OwnerId = userId,
            Status = ProjectStatus.Draft,
            IsDeleted = false,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _db.BlastProjects.Add(project);
        await _db.SaveChangesAsync();

        var result = new ProjectDetailDto
        {
            Id = project.Id,
            Name = project.Name,
            SiteLocation = project.SiteLocation,
            BlastType = project.BlastType,
            Status = project.Status.ToString(),
            CreatedAtUtc = project.CreatedAtUtc,
            UpdatedAtUtc = project.UpdatedAtUtc
        };

        return CreatedAtAction(nameof(GetProject), new { id = project.Id }, result);
    }

    // ---------------------------------------------------------------------
    // PUT /api/projects/{id}
    // ---------------------------------------------------------------------
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateProject(
        int id, [FromBody] UpdateProjectDto dto)
    {
        var userId = User.GetUserId();
        var companyId = User.GetCompanyId();
        if (userId is null || companyId is null) return Unauthorized();

        var project = await _db.BlastProjects
            .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId);

        if (project is null) return NotFound();

        // Ownership check
        var isMainCompanyUser = User.IsInRole("MainCompanyUser");
        if (!isMainCompanyUser && project.OwnerId != userId)
            return Forbid();

        // Update only supplied fields
        if (!string.IsNullOrWhiteSpace(dto.Name))
        {
            if (dto.Name.Length > 150)
                return BadRequest(new { message = "Project name cannot exceed 150 characters." });
            project.Name = dto.Name.Trim();
        }

        if (!string.IsNullOrWhiteSpace(dto.SiteLocation))
        {
            if (dto.SiteLocation.Length > 250)
                return BadRequest(new { message = "Site location cannot exceed 250 characters." });
            project.SiteLocation = dto.SiteLocation.Trim();
        }

        if (!string.IsNullOrWhiteSpace(dto.BlastType))
        {
            if (dto.BlastType.Length > 100)
                return BadRequest(new { message = "Blast type cannot exceed 100 characters." });
            project.BlastType = dto.BlastType.Trim();
        }

        if (dto.RockType is not null)
        {
            if (dto.RockType.Length > 100)
                return BadRequest(new { message = "Rock type cannot exceed 100 characters." });
            project.RockType = dto.RockType;
        }

        if (dto.RockDensity.HasValue)
        {
            if (dto.RockDensity.Value <= 0)
                return BadRequest(new { message = "Rock density must be greater than zero." });
            project.RockDensity = dto.RockDensity;
        }

        if (dto.Burden.HasValue)
        {
            if (dto.Burden.Value <= 0)
                return BadRequest(new { message = "Burden must be greater than zero." });
            project.Burden = dto.Burden;
        }

        if (dto.Spacing.HasValue)
        {
            if (dto.Spacing.Value <= 0)
                return BadRequest(new { message = "Spacing must be greater than zero." });
            project.Spacing = dto.Spacing;
        }

        if (dto.VibrationThreshold.HasValue)
        {
            if (dto.VibrationThreshold.Value <= 0)
                return BadRequest(new { message = "Vibration threshold must be greater than zero." });
            project.VibrationThreshold = dto.VibrationThreshold;
        }

        project.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ---------------------------------------------------------------------
    // DELETE /api/projects/{id}
    // Soft delete (per the developer guide).
    // ---------------------------------------------------------------------
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteProject(int id)
    {
        var userId = User.GetUserId();
        var companyId = User.GetCompanyId();
        if (userId is null || companyId is null) return Unauthorized();

        var project = await _db.BlastProjects
            .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId);

        if (project is null) return NotFound();

        var isMainCompanyUser = User.IsInRole("MainCompanyUser");
        if (!isMainCompanyUser && project.OwnerId != userId)
            return Forbid();

        project.IsDeleted = true;
        project.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }
}