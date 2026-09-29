namespace BlastPro.Mvc.Models.Dtos;

public sealed class CalculationWarningDto
{
    public string Code { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public sealed class CalculationPatternSnapshotDto
{
    public string SiteLocation { get; set; } = "";
    public string BlastType { get; set; } = "";
    public string RockType { get; set; } = "";
    public decimal? BenchLengthMetres { get; set; }
    public decimal? BenchWidthMetres { get; set; }
    public decimal? BurdenMetres { get; set; }
    public decimal? SpacingMetres { get; set; }
    public decimal? VibrationThresholdMmPerSecond { get; set; }
    public List<CalculationHoleSnapshotDto> Holes { get; set; } = new();
}

public sealed class CalculationHoleSnapshotDto
{
    public int Number { get; set; }
    public decimal X { get; set; }
    public decimal Y { get; set; }
    public decimal Depth { get; set; }
    public decimal? DiameterMillimetres { get; set; }
    public decimal? SubdrillMetres { get; set; }
    public string ProductName { get; set; } = "";
    public decimal? ProductDensityGramsPerCc { get; set; }
    public decimal Charge { get; set; }
    public decimal Stemming { get; set; }
    public int Delay { get; set; }
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
    public CalculationPatternSnapshotDto? PatternSnapshot { get; set; }
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
