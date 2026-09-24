using System.Globalization;

namespace BlastPro.Mvc.Models.ViewModels.Results;

public sealed class ResultsViewModel
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public bool HasResults { get; set; }
    public int? ResultId { get; set; }
    public bool ViewingHistory { get; set; }
    public bool IsOutdated { get; set; }
    public DateTime? CalculatedAtUtc { get; set; }
    public string? CalculatedByName { get; set; }
    public int TotalHoles { get; set; }
    public decimal TotalExplosiveKg { get; set; }
    public decimal TotalDrillingMetres { get; set; }
    public decimal? TotalCost { get; set; }
    public string? CurrencyCode { get; set; }
    public decimal MaxChargePerDelayKg { get; set; }
    public decimal? PowderFactorKgPerTonne { get; set; }
    public decimal? EstimatedVolumeCubicMetres { get; set; }
    public decimal? EstimatedTonnageTonnes { get; set; }
    public decimal? PredictedPpvMmPerSecond { get; set; }
    public decimal? PredictedFlyrockMetres { get; set; }
    public List<ResultWarningViewModel> Warnings { get; set; } = new();
    public List<PreviousResultViewModel> PreviousResults { get; set; } = new();

    public int WarningCount => Warnings.Count;
    public int CriticalWarningCount => Warnings.Count(w => w.Severity == "Critical");

    public string FormatNumber(decimal value, int decimals = 0) =>
        value.ToString(decimals == 0 ? "N0" : "N" + decimals, CultureInfo.InvariantCulture);

    public string FormatCost(decimal? amount, string? currencyCode) =>
        amount is null || string.IsNullOrWhiteSpace(currencyCode)
            ? "Not available"
            : $"{currencyCode.ToUpperInvariant()} {FormatNumber(amount.Value, 2)}";
}

public sealed class ResultWarningViewModel
{
    public string Code { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public sealed class PreviousResultViewModel
{
    public int Id { get; set; }
    public DateTime CalculatedAtUtc { get; set; }
    public string? CalculatedByName { get; set; }
    public decimal? TotalCost { get; set; }
    public string? CurrencyCode { get; set; }
    public decimal? PowderFactorKgPerTonne { get; set; }
    public int WarningCount { get; set; }
}
