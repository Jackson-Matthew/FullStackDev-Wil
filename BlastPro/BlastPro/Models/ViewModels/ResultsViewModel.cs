
using System.Globalization;

namespace BlastPro.Mvc.Models.ViewModels.Results;

// sample data needs the api and data from backend later on
public class ResultsViewModel
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;

    public bool HasResults { get; set; }

    public bool IsOutdated { get; set; }

    public DateTime? CalculatedAtUtc { get; set; }
    public string? CalculatedByName { get; set; }


    public int TotalHoles { get; set; }
    public decimal TotalExplosiveKg { get; set; }
    public decimal TotalDrillingMetres { get; set; }

    public decimal? TotalCost { get; set; }
    public string? CurrencyCode { get; set; }

    public decimal MaxChargePerDelayKg { get; set; }

    
    public int MaxChargeSafetyIndexPercent { get; set; }

    //low , med or high
    public string MaxChargeSeverityLabel { get; set; } = "Low";

    // range bar

    public decimal? PowderFactorKgPerTonne { get; set; }
    public decimal PowderFactorScaleMin { get; set; } = 0.1m;
    public decimal PowderFactorScaleMax { get; set; } = 0.8m;
    public decimal? PowderFactorTargetMin { get; set; }
    public decimal? PowderFactorTargetMax { get; set; }


    public decimal? EstimatedVolumeCubicMetres { get; set; }
    public decimal? EstimatedTonnageTonnes { get; set; }
    public decimal? PredictedPpvMmPerSecond { get; set; }
    public decimal? IdealizedFlyrockRangeMetres { get; set; }


    public List<ResultWarningViewModel> Warnings { get; set; } = new();

    // history or the old calculated results

    public List<PreviousResultViewModel> PreviousResults { get; set; } = new();

    

    public string CurrencyPrefix =>
        (CurrencyCode ?? "ZAR").ToUpperInvariant() switch
        {
            "ZAR" => "R",
            "USD" => "$",
            "EUR" => "€",
            "GBP" => "£",
            _ => (CurrencyCode ?? string.Empty) + " "
        };

    public int WarningCount => Warnings.Count;
    public int CriticalWarningCount => Warnings.Count(w => w.Severity == "Critical");

    public string FormatNumber(decimal value, int decimals = 0) =>
        value.ToString(decimals == 0 ? "N0" : "N" + decimals, CultureInfo.InvariantCulture);

    //this is for the range bar
    public double PowderFactorPositionPercent
    {
        get
        {
            if (PowderFactorKgPerTonne is null) return 0;
            var range = PowderFactorScaleMax - PowderFactorScaleMin;
            if (range <= 0) return 0;
            var pos = (PowderFactorKgPerTonne.Value - PowderFactorScaleMin) / range;
            return (double)Math.Clamp(pos, 0, 1) * 100;
        }
    }
}

public class ResultWarningViewModel
{
    public string Code { get; set; } = string.Empty;

    //for the warining severitity
    public string Severity { get; set; } = "Warning";

    public string Message { get; set; } = string.Empty;
}

public class PreviousResultViewModel
{
    public int Id { get; set; }
    public DateTime CalculatedAtUtc { get; set; }
    public string? CalculatedByName { get; set; }
    public decimal? TotalCost { get; set; }
    public decimal? PowderFactorKgPerTonne { get; set; }
    public int WarningCount { get; set; }
}