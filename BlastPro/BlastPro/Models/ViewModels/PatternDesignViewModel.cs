namespace BlastPro.Mvc.Models.ViewModels.Projects;

public class PatternDesignViewModel
{
    public string ProjectId { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft";
    public string RockType { get; set; } = string.Empty;
    public decimal RockDensity { get; set; }
    public decimal Burden { get; set; }
    public decimal Spacing { get; set; }
    public decimal VibrationThreshold { get; set; }
    public List<BlastHoleViewModel> Holes { get; set; } = new();

    public static IReadOnlyList<string> RockTypes { get; } =
    [
        "Granite",
        "Limestone",
        "Sandstone",
        "Shale",
        "Other"
    ];

    public static IReadOnlyList<string> Explosives { get; } =
    [
        "ANFO Pack",
        "Emulsion Max"
    ];
}

public class BlastHoleViewModel
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
