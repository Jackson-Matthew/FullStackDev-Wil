using System.Globalization;

namespace BlastPro.Mvc.Models.ViewModels.Reports;

public class ReportViewModel
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public DateTime ReportDateUtc { get; set; }


    public string CompanyName { get; set; } = string.Empty;
    public string CalculationsUnit { get; set; } = "Metric System";


    public bool HasResults { get; set; }

    // hole pattern data

    public List<ReportHoleViewModel> Holes { get; set; } = new();

    // design parameters

    public int TotalDesignatedHoles { get; set; }
    public decimal TotalExplosiveLoadKg { get; set; }
    public decimal TotalDrillingLengthMetres { get; set; }
    public decimal? TotalEstimatedTonnageTonnes { get; set; }
    public decimal? PowderFactorKgPerTonne { get; set; }
    public decimal? TotalEstimatedMaterialCost { get; set; }
    public string? CurrencyCode { get; set; }


    public decimal MaxChargePerDelayKg { get; set; }
    public decimal? PredictedPpvMmPerSecond { get; set; }
    public decimal? IdealizedFlyrockRangeMetres { get; set; }

    // Safety alerts

    public List<ReportWarningViewModel> Warnings { get; set; } = new();


    public string CurrencyPrefix =>
        (CurrencyCode ?? "ZAR").ToUpperInvariant() switch
        {
            "ZAR" => "R",
            "USD" => "$",
            "EUR" => "€",
            "GBP" => "£",
            _ => (CurrencyCode ?? string.Empty) + " "
        };

    public string FormatNumber(decimal value, int decimals = 0) =>
        value.ToString(decimals == 0 ? "N0" : "N" + decimals, CultureInfo.InvariantCulture);
}

public class ReportHoleViewModel
{
    public int Number { get; set; }
    public decimal X { get; set; }
    public decimal Y { get; set; }
    public decimal Depth { get; set; }
    public string Explosive { get; set; } = string.Empty;
    public decimal Charge { get; set; }
    public decimal Stemming { get; set; }
    public int Delay { get; set; }
}

public class ReportWarningViewModel
{
    public string Severity { get; set; } = "Warning";

    public string Message { get; set; } = string.Empty;
}
