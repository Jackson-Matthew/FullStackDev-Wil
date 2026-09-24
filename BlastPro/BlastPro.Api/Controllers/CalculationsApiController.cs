using BlastPro.Api.Data;
using BlastPro.Api.Extensions;
using BlastPro.Api.Models.Dtos;
using BlastPro.Api.Models.Entities;
using BlastPro.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlastPro.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/projects/{id:int}")]
public sealed class CalculationsApiController(
    ApplicationDbContext db,
    ILogger<CalculationsApiController> logger) : ControllerBase
{
    [HttpPost("calculations")]
    public async Task<ActionResult<CalculationResultDto>> RunCalculation(int id, [FromBody] RunCalculationRequest request)
    {
        var userId = User.GetUserId();
        var companyId = User.GetCompanyId();
        if (userId is null || companyId is null) return Unauthorized();

        var project = await db.BlastProjects
            .Include(p => p.Holes)
            .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId);
        if (project is null || (!User.IsInRole("MainCompanyUser") && project.OwnerId != userId))
            return NotFound();

        var validationError = Validate(request);
        if (validationError is not null) return BadRequest(new { message = validationError });

        var productIds = project.Holes
            .Select(h => h.ExplosiveProductId ?? project.ExplosiveProductId)
            .Where(productId => productId.HasValue)
            .Select(productId => productId!.Value)
            .Distinct()
            .ToArray();
        var products = await db.ExplosiveProducts
            .Where(p => productIds.Contains(p.Id) && p.CompanyId == companyId)
            .ToListAsync();
        if (products.Count != productIds.Length)
            return BadRequest(new { message = "A selected explosive product is unavailable to this company." });

        try
        {
            var settings = new CalculationSettings
            {
                DelayWindowMilliseconds = request.DelayWindowMilliseconds,
                SubdrillMetres = request.SubdrillMetres,
                ReceptorDistanceMetres = request.ReceptorDistanceMetres,
                PpvSiteCoefficient = request.PpvSiteCoefficient,
                PpvDecayExponent = request.PpvDecayExponent,
                FlyrockLaunchSpeedMetresPerSecond = request.FlyrockLaunchSpeedMetresPerSecond,
                FlyrockLaunchAngleDegrees = request.FlyrockLaunchAngleDegrees,
                FlyrockLaunchHeightMetres = request.FlyrockLaunchHeightMetres,
                ExclusionRadiusMetres = request.ExclusionRadiusMetres
            };
            var summary = CalculationService.CalculateAll(project, project.Holes, products, settings);
            var now = DateTime.UtcNow;

            // SQL Server keeps replacement and insert in one transaction.
            // The in-memory test provider does not support transactions.
            await using var transaction = db.Database.IsRelational()
                ? await db.Database.BeginTransactionAsync()
                : null;

            if (db.Database.IsRelational())
            {
                await db.CalculationResults
                    .Where(r => r.BlastProjectId == id && r.IsCurrent)
                    .ExecuteUpdateAsync(update => update.SetProperty(r => r.IsCurrent, false));
            }
            else
            {
                var previous = await db.CalculationResults
                    .Where(r => r.BlastProjectId == id && r.IsCurrent)
                    .ToListAsync();
                foreach (var old in previous) old.IsCurrent = false;
            }

            var result = new CalculationResult
            {
                BlastProjectId = id,
                CalculatedByUserId = userId,
                TotalHoles = summary.TotalHoles,
                TotalExplosiveKg = summary.TotalExplosiveKg,
                TotalDrillingMetres = summary.TotalDrillingMetres,
                EstimatedVolumeCubicMetres = summary.EstimatedVolumeCubicMetres,
                EstimatedTonnageTonnes = summary.EstimatedTonnageTonnes,
                TotalCost = summary.TotalCost,
                CurrencyCode = summary.CurrencyCode,
                MaxChargePerDelayKg = summary.MaxChargePerDelayKg,
                PowderFactorKgPerTonne = summary.PowderFactorKgPerTonne,
                PredictedPpvMmPerSecond = summary.PredictedPpvMmPerSecond,
                PredictedFlyrockMetres = summary.IdealizedFlyrockRangeMetres,
                IsCurrent = true,
                CalculatedAtUtc = now,
                Warnings = summary.Warnings.Select(w => new BlastWarning
                {
                    Code = w.Code,
                    Severity = w.Severity,
                    Message = w.Message,
                    CreatedAtUtc = now
                }).ToList()
            };
            db.CalculationResults.Add(result);
            await db.SaveChangesAsync();
            if (transaction is not null) await transaction.CommitAsync();

            var userName = await db.Users.Where(u => u.Id == userId)
                .Select(u => u.FullName).SingleAsync();
            return CreatedAtAction(nameof(GetResult), new { id, resultId = result.Id },
                MapResult(result, project.Name, userName));
        }
        catch (Exception ex) when (ex is ArgumentException or OverflowException)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not save calculation for project {ProjectId} and user {UserId}", id, userId);
            return StatusCode(500, new { message = "The calculation could not be saved. Please try again." });
        }
    }

    [HttpGet("results")]
    public async Task<ActionResult<ProjectResultsDto>> GetResults(int id)
    {
        var userId = User.GetUserId();
        var companyId = User.GetCompanyId();
        if (userId is null || companyId is null) return Unauthorized();

        var project = await db.BlastProjects.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId);
        if (project is null || (!User.IsInRole("MainCompanyUser") && project.OwnerId != userId))
            return NotFound();

        var results = await db.CalculationResults.AsNoTracking()
            .Where(r => r.BlastProjectId == id)
            .Include(r => r.CalculatedByUser)
            .Include(r => r.Warnings)
            .OrderByDescending(r => r.CalculatedAtUtc)
            .ThenByDescending(r => r.Id)
            .ToListAsync();
        var displayed = results.FirstOrDefault(r => r.IsCurrent) ?? results.FirstOrDefault();
        return Ok(new ProjectResultsDto
        {
            ProjectId = id,
            ProjectName = project.Name,
            HasResults = displayed is not null,
            IsOutdated = displayed is not null && !displayed.IsCurrent,
            Result = displayed is null ? null : MapResult(displayed, project.Name, displayed.CalculatedByUser.FullName),
            History = results.Where(r => r.Id != displayed?.Id)
                .Select(r => new CalculationHistoryDto
                {
                    Id = r.Id,
                    CalculatedAtUtc = r.CalculatedAtUtc,
                    CalculatedByName = r.CalculatedByUser.FullName,
                    TotalCost = r.TotalCost,
                    CurrencyCode = r.CurrencyCode,
                    PowderFactorKgPerTonne = r.PowderFactorKgPerTonne,
                    WarningCount = r.Warnings.Count
                }).ToList()
        });
    }

    [HttpGet("results/{resultId:int}")]
    public async Task<ActionResult<CalculationResultDto>> GetResult(int id, int resultId)
    {
        var userId = User.GetUserId();
        var companyId = User.GetCompanyId();
        if (userId is null || companyId is null) return Unauthorized();

        var project = await db.BlastProjects.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId);
        if (project is null || (!User.IsInRole("MainCompanyUser") && project.OwnerId != userId))
            return NotFound();

        var result = await db.CalculationResults.AsNoTracking()
            .Include(r => r.CalculatedByUser)
            .Include(r => r.Warnings)
            .FirstOrDefaultAsync(r => r.Id == resultId && r.BlastProjectId == id);
        return result is null ? NotFound() : Ok(MapResult(result, project.Name, result.CalculatedByUser.FullName));
    }

    private static string? Validate(RunCalculationRequest request)
    {
        if (request.DelayWindowMilliseconds <= 0) return "Enter a delay window greater than zero milliseconds.";
        if (request.SubdrillMetres is < 0) return "Subdrill cannot be negative.";
        if (request.ReceptorDistanceMetres is <= 0) return "Receptor distance must be positive.";
        if (request.PpvSiteCoefficient is <= 0) return "PPV site coefficient must be positive.";
        if (request.PpvDecayExponent is <= 0) return "PPV decay exponent must be positive.";
        if (request.FlyrockLaunchSpeedMetresPerSecond is <= 0) return "Flyrock launch speed must be positive.";
        if (request.FlyrockLaunchAngleDegrees is < 0 or > 90) return "Flyrock launch angle must be between 0 and 90 degrees.";
        if (request.FlyrockLaunchHeightMetres is < 0) return "Flyrock launch height cannot be negative.";
        if (request.ExclusionRadiusMetres is <= 0) return "Exclusion radius must be positive.";
        return null;
    }

    private static CalculationResultDto MapResult(CalculationResult result, string projectName, string userName) => new()
    {
        Id = result.Id,
        ProjectId = result.BlastProjectId,
        ProjectName = projectName,
        CalculatedAtUtc = result.CalculatedAtUtc,
        CalculatedByName = userName,
        IsCurrent = result.IsCurrent,
        IsOutdated = !result.IsCurrent,
        TotalHoles = result.TotalHoles,
        TotalExplosiveKg = result.TotalExplosiveKg,
        TotalDrillingMetres = result.TotalDrillingMetres,
        EstimatedVolumeCubicMetres = result.EstimatedVolumeCubicMetres,
        EstimatedTonnageTonnes = result.EstimatedTonnageTonnes,
        TotalCost = result.TotalCost,
        CurrencyCode = result.CurrencyCode,
        MaxChargePerDelayKg = result.MaxChargePerDelayKg,
        PowderFactorKgPerTonne = result.PowderFactorKgPerTonne,
        PredictedPpvMmPerSecond = result.PredictedPpvMmPerSecond,
        PredictedFlyrockMetres = result.PredictedFlyrockMetres,
        Warnings = result.Warnings.Select(w => new CalculationWarningDto
        {
            Code = w.Code,
            Severity = w.Severity.ToString(),
            Message = w.Message
        }).ToList()
    };
}
