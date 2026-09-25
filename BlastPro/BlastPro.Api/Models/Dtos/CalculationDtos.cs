namespace BlastPro.Api.Models.Dtos;

public sealed class RunCalculationRequest
{
    public int DelayWindowMilliseconds { get; init; }
    public decimal? SubdrillMetres { get; init; }
    public decimal? ReceptorDistanceMetres { get; init; }
    public decimal? PpvSiteCoefficient { get; init; }
    public decimal? PpvDecayExponent { get; init; }
    public decimal? FlyrockLaunchSpeedMetresPerSecond { get; init; }
    public decimal? FlyrockLaunchAngleDegrees { get; init; }
    public decimal? FlyrockLaunchHeightMetres { get; init; }
    public decimal? ExclusionRadiusMetres { get; init; }
}

public sealed class CalculationWarningDto
{
    public string Code { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

public sealed class CalculationResultDto
{
    public int Id { get; init; }
    public int ProjectId { get; init; }
    public string ProjectName { get; init; } = string.Empty;
    public DateTime CalculatedAtUtc { get; init; }
    public string CalculatedByName { get; init; } = string.Empty;
    public bool IsCurrent { get; init; }
    public bool IsOutdated { get; init; }
    public int TotalHoles { get; init; }
    public decimal TotalExplosiveKg { get; init; }
    public decimal TotalDrillingMetres { get; init; }
    public decimal? EstimatedVolumeCubicMetres { get; init; }
    public decimal? EstimatedTonnageTonnes { get; init; }
    public decimal? TotalCost { get; init; }
    public string? CurrencyCode { get; init; }
    public decimal MaxChargePerDelayKg { get; init; }
    public decimal? PowderFactorKgPerTonne { get; init; }
    public decimal? PredictedPpvMmPerSecond { get; init; }
    public decimal? PredictedFlyrockMetres { get; init; }
    public List<CalculationWarningDto> Warnings { get; init; } = new();
}

public sealed class CalculationHistoryDto
{
    public int Id { get; init; }
    public DateTime CalculatedAtUtc { get; init; }
    public string CalculatedByName { get; init; } = string.Empty;
    public decimal? TotalCost { get; init; }
    public string? CurrencyCode { get; init; }
    public decimal? PowderFactorKgPerTonne { get; init; }
    public int WarningCount { get; init; }
}

public sealed class ProjectResultsDto
{
    public int ProjectId { get; init; }
    public string ProjectName { get; init; } = string.Empty;
    public bool HasResults { get; init; }
    public bool IsOutdated { get; init; }
    public CalculationResultDto? Result { get; init; }
    public List<CalculationHistoryDto> History { get; init; } = new();
}
