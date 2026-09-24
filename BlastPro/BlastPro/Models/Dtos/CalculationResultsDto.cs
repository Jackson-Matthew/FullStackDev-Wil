namespace BlastPro.Mvc.Models.Dtos;

public sealed class CalculationWarningDto
{
    public string Code { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public sealed class CalculationResultDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public DateTime CalculatedAtUtc { get; set; }
    public string CalculatedByName { get; set; } = string.Empty;
    public bool IsCurrent { get; set; }
    public bool IsOutdated { get; set; }
    public int TotalHoles { get; set; }
    public decimal TotalExplosiveKg { get; set; }
    public decimal TotalDrillingMetres { get; set; }
    public decimal? EstimatedVolumeCubicMetres { get; set; }
    public decimal? EstimatedTonnageTonnes { get; set; }
    public decimal? TotalCost { get; set; }
    public string? CurrencyCode { get; set; }
    public decimal MaxChargePerDelayKg { get; set; }
    public decimal? PowderFactorKgPerTonne { get; set; }
    public decimal? PredictedPpvMmPerSecond { get; set; }
    public decimal? PredictedFlyrockMetres { get; set; }
    public List<CalculationWarningDto> Warnings { get; set; } = new();
}

public sealed class CalculationHistoryDto
{
    public int Id { get; set; }
    public DateTime CalculatedAtUtc { get; set; }
    public string CalculatedByName { get; set; } = string.Empty;
    public decimal? TotalCost { get; set; }
    public string? CurrencyCode { get; set; }
    public decimal? PowderFactorKgPerTonne { get; set; }
    public int WarningCount { get; set; }
}

public sealed class ProjectResultsDto
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public bool HasResults { get; set; }
    public bool IsOutdated { get; set; }
    public CalculationResultDto? Result { get; set; }
    public List<CalculationHistoryDto> History { get; set; } = new();
}
