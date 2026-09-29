using BlastPro.Api.Data;
using BlastPro.Api.Extensions;
using BlastPro.Api.Models.Dtos;
using BlastPro.Api.Models.Entities;
using BlastPro.Api.Models.Enums;
using BlastPro.Api.Services;
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

        var oldRockDensity = project.RockDensity;
        var oldBurden = project.Burden;
        var oldSpacing = project.Spacing;
        var oldVibrationThreshold = project.VibrationThreshold;

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
            project.VibrationThresholdMode = "Manual";
        }

        project.UpdatedAtUtc = DateTime.UtcNow;

        if (oldRockDensity != project.RockDensity || oldBurden != project.Burden ||
            oldSpacing != project.Spacing || oldVibrationThreshold != project.VibrationThreshold)
            await InvalidateResults(id);

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ---------------------------------------------------------------------
    // GET /api/projects/{id}/pattern-design
    // Loads the project fields, holes and active company explosive products.
    // ---------------------------------------------------------------------
    [HttpGet("{id:int}/pattern-design")]
    public async Task<ActionResult<PatternDesignDto>> GetPatternDesign(int id)
    {
        var userId = User.GetUserId();
        var companyId = User.GetCompanyId();
        if (userId is null || companyId is null) return Unauthorized();

        var project = await _db.BlastProjects
            .Include(p => p.Holes)
            .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId);

        if (project is null) return NotFound();
        if (!User.IsInRole("MainCompanyUser") && project.OwnerId != userId)
            return NotFound();

        return Ok(await BuildPatternDesignDto(project));
    }

    // ---------------------------------------------------------------------
    // PUT /api/projects/{id}/pattern-design
    // Saves the project parameters and complete hole collection atomically.
    // ---------------------------------------------------------------------
    [HttpPut("{id:int}/pattern-design")]
    public async Task<ActionResult<PatternDesignDto>> SavePatternDesign(
        int id, [FromBody] SavePatternDesignDto dto)
    {
        var userId = User.GetUserId();
        var companyId = User.GetCompanyId();
        if (userId is null || companyId is null) return Unauthorized();

        var project = await _db.BlastProjects
            .Include(p => p.Holes)
            .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId);

        if (project is null) return NotFound();
        if (!User.IsInRole("MainCompanyUser") && project.OwnerId != userId)
            return NotFound();

        if (string.IsNullOrWhiteSpace(dto.RockType) || dto.RockType.Length > 100)
            return BadRequest(new { message = "Select a valid rock type." });
        if (dto.RockDensity < 0)
            return BadRequest(new { message = "Rock density cannot be negative." });
        if (dto.Burden < 0 || dto.Burden is > 0 and < 0.0001m)
            return BadRequest(new { message = "Burden must be zero while pending or at least 0.0001 m." });
        if (dto.Spacing < 0 || dto.Spacing is > 0 and < 0.0001m)
            return BadRequest(new { message = "Spacing must be zero while pending or at least 0.0001 m." });
        if (dto.BenchLengthMetres is <= 0 or > 10000 || dto.BenchWidthMetres is <= 0 or > 10000)
            return BadRequest(new { message = "Bench dimensions must be greater than zero and no more than 10,000 metres." });
        if (dto.LayoutRows is <= 0 or > 500 || dto.LayoutColumns is <= 0 or > 500 ||
            (dto.LayoutRows.HasValue && dto.LayoutColumns.HasValue &&
                (long)dto.LayoutRows.Value * dto.LayoutColumns.Value > 500))
            return BadRequest(new { message = "Row and column counts must be whole numbers from 1 to 500 and total at most 500 holes." });
        if (dto.TimingOrder is not ("Sequential" or "Rows" or "Columns" or "Serpentine" or "Chevron" or "Echelon" or "HalfRowOverlap" or "RowGroups" or "ColumnGroups") ||
            dto.TimingIntervalMilliseconds is <= 0 ||
            (dto.TimingIntervalMilliseconds.HasValue &&
                (long)Math.Max(0, dto.Holes.Count - 1) * dto.TimingIntervalMilliseconds.Value > int.MaxValue))
            return BadRequest(new { message = "Select a supported timing order and a valid site interval." });
        if (dto.PatternType is not ("Rectangular" or "Staggered"))
            return BadRequest(new { message = "Select a supported pattern type." });
        if (dto.ReferenceExplosiveFamily is not ("" or "S100" or "PowergelEco" or "PowergelX2" or "S300" or "S300Supreme" or "S300Volcano"))
            return BadRequest(new { message = "Select a supported product reference family." });
        if (!string.IsNullOrEmpty(dto.DefaultAeciProductCode) &&
            AeciSurfaceProductCatalog.Find(dto.DefaultAeciProductCode) is null)
            return BadRequest(new { message = "Select an AECI product from the catalogue." });
        if (dto.LoadingDensityGramsPerCc is <= 0 or > 5)
            return BadRequest(new { message = "Enter a positive actual in-hole density no greater than 5 g/cm³." });
        var calculation = dto.Calculation;
        if (calculation?.DelayWindowMilliseconds is <= 0)
            return BadRequest(new { message = "Delay window must be positive." });
        if (calculation?.SubdrillMetres is < 0)
            return BadRequest(new { message = "Subdrill cannot be negative." });
        if (calculation?.ReceptorDistanceMetres is <= 0)
            return BadRequest(new { message = "Receptor distance must be positive." });
        if (calculation?.PpvSiteCoefficient is <= 0 || calculation?.PpvDecayExponent is <= 0)
            return BadRequest(new { message = "PPV site coefficient and decay exponent must be positive." });
        if (calculation?.FlyrockLaunchSpeedMetresPerSecond is <= 0 ||
            calculation?.FlyrockLaunchAngleDegrees is < 0 or > 90 ||
            calculation?.FlyrockLaunchHeightMetres is < 0)
            return BadRequest(new { message = "Check flyrock launch speed, angle and height." });
        if (calculation?.ExclusionRadiusMetres is <= 0)
            return BadRequest(new { message = "Exclusion radius must be positive." });
        if (dto.ReceptorStructureType is not ("Unspecified" or "ResidentialPlaster" or "ResidentialDrywall" or "Other") ||
            dto.VibrationThresholdMode is not ("Manual" or "Automatic") ||
            dto.DominantFrequencyHz is <= 0 or > 100)
            return BadRequest(new { message = "Check the receptor type, vibration mode and frequency (0–100 Hz)." });
        var vibrationThreshold = dto.VibrationThresholdMode == "Automatic"
            ? UsbMResidentialVibrationGuidance.SuggestedLimitMmPerSecond(
                dto.ReceptorStructureType, dto.DominantFrequencyHz)
            : dto.VibrationThreshold;
        if (dto.VibrationThresholdMode == "Automatic" && vibrationThreshold is null or <= 0)
            return BadRequest(new { message = "Choose a supported house type and measured frequency for USBM guidance." });
        if (dto.VibrationThresholdMode == "Manual" && dto.VibrationThreshold < 0)
            return BadRequest(new { message = "Vibration limit cannot be negative." });

        var rockDensity = dto.RockDensity > 0 ? dto.RockDensity : (decimal?)null;
        var burden = dto.Burden > 0 ? dto.Burden : (decimal?)null;
        var spacing = dto.Spacing > 0 ? dto.Spacing : (decimal?)null;
        var storedLimit = vibrationThreshold > 0 ? vibrationThreshold : null;

        if (!string.IsNullOrWhiteSpace(dto.RowVersion))
        {
            byte[] submittedRowVersion;
            try
            {
                submittedRowVersion = Convert.FromBase64String(dto.RowVersion);
            }
            catch (FormatException)
            {
                return BadRequest(new { message = "The project version is invalid. Reload the page and try again." });
            }

            if (!project.RowVersion.SequenceEqual(submittedRowVersion))
                return Conflict(new { message = "This project was changed by someone else. Reload it before saving." });

            _db.Entry(project).Property(p => p.RowVersion).OriginalValue = submittedRowVersion;
        }

        var holeNumbers = new HashSet<int>();
        var submittedHoleIds = new HashSet<int>();
        foreach (var hole in dto.Holes)
        {
            if (hole.Number <= 0 || !holeNumbers.Add(hole.Number))
                return BadRequest(new { message = "Hole numbers must be unique and greater than zero." });
            if (hole.Depth <= 0)
                return BadRequest(new { message = $"Hole {hole.Number} depth must be greater than zero." });
            if (hole.DiameterMillimetres is <= 0 or > 1000)
                return BadRequest(new { message = $"Hole {hole.Number} diameter must be between 0 and 1,000 mm." });
            if (!string.IsNullOrEmpty(hole.AeciProductCode) &&
                AeciSurfaceProductCatalog.Find(hole.AeciProductCode) is null)
                return BadRequest(new { message = $"Hole {hole.Number} has an unknown AECI product." });
            if (hole.ProductDensityGramsPerCc is <= 0 or > 5)
                return BadRequest(new { message = $"Hole {hole.Number} product density must be positive and at most 5 g/cm³." });
            if (hole.SubdrillMetres is < 0 || hole.SubdrillMetres >= hole.Depth)
                return BadRequest(new { message = $"Hole {hole.Number} subdrill must be non-negative and less than its depth." });
            if (hole.Charge < 0)
                return BadRequest(new { message = $"Hole {hole.Number} charge cannot be negative." });
            if (hole.Stemming < 0 || hole.Stemming > hole.Depth)
                return BadRequest(new { message = $"Hole {hole.Number} stemming must be between zero and its depth." });
            if (hole.Delay < 0)
                return BadRequest(new { message = $"Hole {hole.Number} delay cannot be negative." });
            if (hole.Id is > 0 && !submittedHoleIds.Add(hole.Id.Value))
                return BadRequest(new { message = "The same saved hole cannot be submitted more than once." });
        }

        var existingHoleIds = project.Holes.Select(h => h.Id).ToHashSet();
        if (submittedHoleIds.Any(holeId => !existingHoleIds.Contains(holeId)))
            return BadRequest(new { message = "One or more holes do not belong to this project." });

        var productIds = dto.Holes
            .Where(h => string.IsNullOrEmpty(h.AeciProductCode) && h.ExplosiveProductId.HasValue)
            .Select(h => h.ExplosiveProductId!.Value)
            .Distinct()
            .ToArray();

        var validProductCount = await _db.ExplosiveProducts.CountAsync(product =>
            productIds.Contains(product.Id) &&
            product.CompanyId == companyId &&
            product.IsActive);

        if (validProductCount != productIds.Length)
            return BadRequest(new { message = "Select an active explosive product from your company." });

        var inputsChanged = project.RockDensity != rockDensity ||
            project.Burden != burden || project.Spacing != spacing ||
            project.BenchLengthMetres != dto.BenchLengthMetres ||
            project.BenchWidthMetres != dto.BenchWidthMetres ||
            project.LayoutRows != dto.LayoutRows || project.LayoutColumns != dto.LayoutColumns ||
            project.TimingOrder != dto.TimingOrder ||
            project.TimingIntervalMilliseconds != dto.TimingIntervalMilliseconds ||
            project.PatternType != dto.PatternType ||
            project.ReferenceExplosiveFamily != dto.ReferenceExplosiveFamily ||
            project.DefaultAeciProductCode != dto.DefaultAeciProductCode ||
            project.LoadingDensityGramsPerCc != dto.LoadingDensityGramsPerCc ||
            (calculation is not null && (project.DelayWindowMilliseconds != calculation.DelayWindowMilliseconds ||
                project.SubdrillMetres != calculation.SubdrillMetres ||
                project.ReceptorDistanceMetres != calculation.ReceptorDistanceMetres ||
                project.PpvSiteCoefficient != calculation.PpvSiteCoefficient ||
                project.PpvDecayExponent != calculation.PpvDecayExponent ||
                project.FlyrockLaunchSpeedMetresPerSecond != calculation.FlyrockLaunchSpeedMetresPerSecond ||
                project.FlyrockLaunchAngleDegrees != calculation.FlyrockLaunchAngleDegrees ||
                project.FlyrockLaunchHeightMetres != calculation.FlyrockLaunchHeightMetres ||
                project.ExclusionRadiusMetres != calculation.ExclusionRadiusMetres)) ||
            project.VibrationThreshold != storedLimit ||
            project.ReceptorStructureType != dto.ReceptorStructureType ||
            project.DominantFrequencyHz != dto.DominantFrequencyHz ||
            project.VibrationThresholdMode != dto.VibrationThresholdMode ||
            project.Holes.Count != dto.Holes.Count ||
            dto.Holes.Any(submitted =>
            {
                var saved = project.Holes.FirstOrDefault(h => h.Id == submitted.Id);
                return saved is null || saved.HoleNumber != submitted.Number ||
                    saved.XCoordinate != submitted.X || saved.YCoordinate != submitted.Y ||
                    saved.Depth != submitted.Depth || saved.ChargeKg != submitted.Charge ||
                    saved.DiameterMillimetres != submitted.DiameterMillimetres ||
                    saved.AeciProductCode != submitted.AeciProductCode ||
                    saved.ProductDensityGramsPerCc != submitted.ProductDensityGramsPerCc ||
                    saved.SubdrillMetres != submitted.SubdrillMetres ||
                    saved.StemmingMetres != submitted.Stemming ||
                    saved.DelayMilliseconds != submitted.Delay ||
                    saved.ExplosiveProductId != (string.IsNullOrEmpty(submitted.AeciProductCode)
                        ? submitted.ExplosiveProductId : null);
            });

        project.RockType = dto.RockType.Trim();
        project.RockDensity = rockDensity;
        project.Burden = burden;
        project.Spacing = spacing;
        project.BenchLengthMetres = dto.BenchLengthMetres;
        project.BenchWidthMetres = dto.BenchWidthMetres;
        project.LayoutRows = dto.LayoutRows;
        project.LayoutColumns = dto.LayoutColumns;
        project.TimingOrder = dto.TimingOrder;
        project.TimingIntervalMilliseconds = dto.TimingIntervalMilliseconds;
        project.PatternType = dto.PatternType;
        project.ReferenceExplosiveFamily = dto.ReferenceExplosiveFamily;
        project.DefaultAeciProductCode = dto.DefaultAeciProductCode;
        project.LoadingDensityGramsPerCc = dto.LoadingDensityGramsPerCc;
        if (calculation is not null)
        {
            project.DelayWindowMilliseconds = calculation.DelayWindowMilliseconds;
            project.SubdrillMetres = calculation.SubdrillMetres;
            project.ReceptorDistanceMetres = calculation.ReceptorDistanceMetres;
            project.PpvSiteCoefficient = calculation.PpvSiteCoefficient;
            project.PpvDecayExponent = calculation.PpvDecayExponent;
            project.FlyrockLaunchSpeedMetresPerSecond = calculation.FlyrockLaunchSpeedMetresPerSecond;
            project.FlyrockLaunchAngleDegrees = calculation.FlyrockLaunchAngleDegrees;
            project.FlyrockLaunchHeightMetres = calculation.FlyrockLaunchHeightMetres;
            project.ExclusionRadiusMetres = calculation.ExclusionRadiusMetres;
        }
        project.VibrationThreshold = storedLimit;
        project.ReceptorStructureType = dto.ReceptorStructureType;
        project.DominantFrequencyHz = dto.DominantFrequencyHz;
        project.VibrationThresholdMode = dto.VibrationThresholdMode;
        if (inputsChanged)
            project.Status = ProjectStatus.Draft;
        project.UpdatedAtUtc = DateTime.UtcNow;

        foreach (var existingHole in project.Holes
                     .Where(h => !submittedHoleIds.Contains(h.Id))
                     .ToList())
        {
            project.Holes.Remove(existingHole);
            _db.BlastHoles.Remove(existingHole);
        }

        foreach (var submittedHole in dto.Holes)
        {
            var hole = submittedHole.Id is > 0
                ? project.Holes.Single(h => h.Id == submittedHole.Id.Value)
                : new BlastHole
                {
                    BlastProjectId = project.Id,
                    CreatedAtUtc = DateTime.UtcNow
                };

            hole.HoleNumber = submittedHole.Number;
            hole.XCoordinate = submittedHole.X;
            hole.YCoordinate = submittedHole.Y;
            hole.Depth = submittedHole.Depth;
            hole.DiameterMillimetres = submittedHole.DiameterMillimetres;
            hole.AeciProductCode = submittedHole.AeciProductCode;
            hole.ProductDensityGramsPerCc = submittedHole.ProductDensityGramsPerCc;
            hole.SubdrillMetres = submittedHole.SubdrillMetres;
            hole.ExplosiveProductId = string.IsNullOrEmpty(submittedHole.AeciProductCode)
                ? submittedHole.ExplosiveProductId : null;
            hole.ChargeKg = submittedHole.Charge;
            hole.StemmingMetres = submittedHole.Stemming;
            hole.DelayMilliseconds = submittedHole.Delay;
            hole.UpdatedAtUtc = DateTime.UtcNow;

            if (submittedHole.Id is not > 0)
                project.Holes.Add(hole);
        }

        if (inputsChanged)
            await InvalidateResults(id);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "This project was changed by someone else. Reload it before saving." });
        }
        catch (DbUpdateException)
        {
            return BadRequest(new { message = "The hole pattern could not be saved. Check the hole numbers and values." });
        }

        return Ok(await BuildPatternDesignDto(project));
    }

    private async Task InvalidateResults(int projectId)
    {
        var currentResults = await _db.CalculationResults
            .Where(result => result.BlastProjectId == projectId && result.IsCurrent)
            .ToListAsync();
        foreach (var result in currentResults)
            result.IsCurrent = false;
    }

    private async Task<PatternDesignDto> BuildPatternDesignDto(BlastProject project)
    {
        var products = await _db.ExplosiveProducts
            .Where(product => product.CompanyId == project.CompanyId && product.IsActive)
            .OrderBy(product => product.Name)
            .Select(product => new ExplosiveProductOptionDto
            {
                Id = product.Id,
                Name = product.Name
            })
            .ToListAsync();

        return new PatternDesignDto
        {
            ProjectId = project.Id,
            ProjectName = project.Name,
            Status = project.Status.ToString(),
            RockType = project.RockType ?? string.Empty,
            RockDensity = project.RockDensity ?? 0,
            Burden = project.Burden ?? 0,
            Spacing = project.Spacing ?? 0,
            BenchLengthMetres = project.BenchLengthMetres,
            BenchWidthMetres = project.BenchWidthMetres,
            LayoutRows = project.LayoutRows,
            LayoutColumns = project.LayoutColumns,
            TimingOrder = project.TimingOrder,
            TimingIntervalMilliseconds = project.TimingIntervalMilliseconds,
            PatternType = project.PatternType,
            ReferenceExplosiveFamily = project.ReferenceExplosiveFamily,
            DefaultAeciProductCode = project.DefaultAeciProductCode,
            LoadingDensityGramsPerCc = project.LoadingDensityGramsPerCc,
            Calculation = new PatternCalculationInputsDto
            {
                DelayWindowMilliseconds = project.DelayWindowMilliseconds,
                SubdrillMetres = project.SubdrillMetres,
                ReceptorDistanceMetres = project.ReceptorDistanceMetres,
                PpvSiteCoefficient = project.PpvSiteCoefficient,
                PpvDecayExponent = project.PpvDecayExponent,
                FlyrockLaunchSpeedMetresPerSecond = project.FlyrockLaunchSpeedMetresPerSecond,
                FlyrockLaunchAngleDegrees = project.FlyrockLaunchAngleDegrees,
                FlyrockLaunchHeightMetres = project.FlyrockLaunchHeightMetres,
                ExclusionRadiusMetres = project.ExclusionRadiusMetres
            },
            VibrationThreshold = project.VibrationThreshold ?? 0,
            ReceptorStructureType = project.ReceptorStructureType,
            DominantFrequencyHz = project.DominantFrequencyHz,
            VibrationThresholdMode = project.VibrationThresholdMode,
            RowVersion = Convert.ToBase64String(project.RowVersion),
            Holes = project.Holes
                .OrderBy(hole => hole.HoleNumber)
                .Select(hole => new PatternHoleDto
                {
                    Id = hole.Id,
                    Number = hole.HoleNumber,
                    X = hole.XCoordinate,
                    Y = hole.YCoordinate,
                    Depth = hole.Depth,
                    DiameterMillimetres = hole.DiameterMillimetres,
                    AeciProductCode = hole.AeciProductCode,
                    ProductDensityGramsPerCc = hole.ProductDensityGramsPerCc,
                    SubdrillMetres = hole.SubdrillMetres,
                    ExplosiveProductId = hole.ExplosiveProductId,
                    Charge = hole.ChargeKg,
                    Stemming = hole.StemmingMetres,
                    Delay = hole.DelayMilliseconds
                })
                .ToList(),
            ExplosiveProducts = products,
            AeciProducts = AeciSurfaceProductCatalog.Products
        };
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
