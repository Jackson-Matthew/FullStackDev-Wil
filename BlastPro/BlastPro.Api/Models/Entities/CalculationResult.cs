namespace BlastPro.Api.Models.Entities;

public sealed class CalculationResult
{
    public int Id { get; set; }
    public int BlastProjectId { get; set; }
    public string CalculatedByUserId { get; set; } = string.Empty;
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
    public bool IsCurrent { get; set; } = true;
    public DateTime CalculatedAtUtc { get; set; }

    public BlastProject BlastProject { get; set; } = null!;
    public ApplicationUser CalculatedByUser { get; set; } = null!;
    public ICollection<BlastWarning> Warnings { get; set; } = new List<BlastWarning>();
}
