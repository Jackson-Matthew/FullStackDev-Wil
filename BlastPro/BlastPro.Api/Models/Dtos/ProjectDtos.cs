namespace BlastPro.Api.Models.Dtos;

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

public class ProjectDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SiteLocation { get; set; } = string.Empty;
    public string BlastType { get; set; } = string.Empty;
    public string? RockType { get; set; }
    public decimal? RockDensity { get; set; }
    public decimal? Burden { get; set; }
    public decimal? Spacing { get; set; }
    public decimal? VibrationThreshold { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class CreateProjectDto
{
    public string Name { get; set; } = string.Empty;
    public string SiteLocation { get; set; } = string.Empty;
    public string BlastType { get; set; } = string.Empty;
}

public class UpdateProjectDto
{
    public string? Name { get; set; }
    public string? SiteLocation { get; set; }
    public string? BlastType { get; set; }
    public string? RockType { get; set; }
    public decimal? RockDensity { get; set; }
    public decimal? Burden { get; set; }
    public decimal? Spacing { get; set; }
    public decimal? VibrationThreshold { get; set; }
}

public class PatternDesignDto
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string RockType { get; set; } = string.Empty;
    public decimal RockDensity { get; set; }
    public decimal Burden { get; set; }
    public decimal Spacing { get; set; }
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
    public PatternCalculationInputsDto Calculation { get; set; } = new();
    public decimal VibrationThreshold { get; set; }
    public string ReceptorStructureType { get; set; } = "Unspecified";
    public decimal? DominantFrequencyHz { get; set; }
    public string VibrationThresholdMode { get; set; } = "Manual";
    public string RowVersion { get; set; } = string.Empty;
    public List<PatternHoleDto> Holes { get; set; } = new();
    public List<ExplosiveProductOptionDto> ExplosiveProducts { get; set; } = new();
    public IReadOnlyList<AeciProductOptionDto> AeciProducts { get; set; } = [];
}

public class PatternHoleDto
{
    public int? Id { get; set; }
    public int Number { get; set; }
    public decimal X { get; set; }
    public decimal Y { get; set; }
    public decimal Depth { get; set; }
    public decimal? DiameterMillimetres { get; set; }
    public decimal? SubdrillMetres { get; set; }
    public int? ExplosiveProductId { get; set; }
    public string AeciProductCode { get; set; } = "";
    public decimal? ProductDensityGramsPerCc { get; set; }
    public decimal Charge { get; set; }
    public decimal Stemming { get; set; }
    public int Delay { get; set; }
}

public class ExplosiveProductOptionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class AeciProductOptionDto
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Series { get; set; } = "";
    public string Application { get; set; } = "";
    public decimal? DensityMinGramsPerCc { get; set; }
    public decimal? DensityMaxGramsPerCc { get; set; }
    public decimal EnergyMinMjPerKg { get; set; }
    public decimal EnergyMaxMjPerKg { get; set; }
    public int? MinimumDiameterMillimetres { get; set; }
}

public class SavePatternDesignDto
{
    public string? RockType { get; set; }
    public decimal RockDensity { get; set; }
    public decimal Burden { get; set; }
    public decimal Spacing { get; set; }
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
    public PatternCalculationInputsDto? Calculation { get; set; }
    public decimal VibrationThreshold { get; set; }
    public string ReceptorStructureType { get; set; } = "Unspecified";
    public decimal? DominantFrequencyHz { get; set; }
    public string VibrationThresholdMode { get; set; } = "Manual";
    public string? RowVersion { get; set; }
    public List<PatternHoleDto> Holes { get; set; } = new();
}

public sealed class PatternCalculationInputsDto
{
    public int? DelayWindowMilliseconds { get; set; }
    public decimal? SubdrillMetres { get; set; }
    public decimal? ReceptorDistanceMetres { get; set; }
    public decimal? PpvSiteCoefficient { get; set; }
    public decimal? PpvDecayExponent { get; set; }
    public decimal? FlyrockLaunchSpeedMetresPerSecond { get; set; }
    public decimal? FlyrockLaunchAngleDegrees { get; set; }
    public decimal? FlyrockLaunchHeightMetres { get; set; }
    public decimal? ExclusionRadiusMetres { get; set; }
}
