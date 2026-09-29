using BlastPro.Api.Models.Enums;

namespace BlastPro.Api.Models.Entities;

public sealed class BlastProject
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public int? ExplosiveProductId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string SiteLocation { get; set; } = string.Empty;
    public string BlastType { get; set; } = string.Empty;
    public string? RockType { get; set; }
    public decimal? RockDensity { get; set; }
    public decimal? Burden { get; set; }
    public decimal? Spacing { get; set; }
    public decimal? BenchLengthMetres { get; set; }
    public decimal? BenchWidthMetres { get; set; }
    public int? LayoutRows { get; set; }
    public int? LayoutColumns { get; set; }
    public string TimingOrder { get; set; } = "Rows";
    public int? TimingIntervalMilliseconds { get; set; }
    public string PatternType { get; set; } = "Rectangular";
    public string ReferenceExplosiveFamily { get; set; } = "";
    public string DefaultAeciProductCode { get; set; } = "";
    public decimal? LoadingDensityGramsPerCc { get; set; }
    public int? DelayWindowMilliseconds { get; set; }
    public decimal? SubdrillMetres { get; set; }
    public decimal? ReceptorDistanceMetres { get; set; }
    public decimal? PpvSiteCoefficient { get; set; }
    public decimal? PpvDecayExponent { get; set; }
    public decimal? FlyrockLaunchSpeedMetresPerSecond { get; set; }
    public decimal? FlyrockLaunchAngleDegrees { get; set; }
    public decimal? FlyrockLaunchHeightMetres { get; set; }
    public decimal? ExclusionRadiusMetres { get; set; }
    public decimal? VibrationThreshold { get; set; }
    public string ReceptorStructureType { get; set; } = "Unspecified";
    public decimal? DominantFrequencyHz { get; set; }
    public string VibrationThresholdMode { get; set; } = "Manual";
    public ProjectStatus Status { get; set; } = ProjectStatus.Draft;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public Company Company { get; set; } = null!;
    public ApplicationUser Owner { get; set; } = null!;
    public ExplosiveProduct? ExplosiveProduct { get; set; }
    public ICollection<BlastHole> Holes { get; set; } = new List<BlastHole>();
    public ICollection<CalculationResult> CalculationResults { get; set; } = new List<CalculationResult>();
}
