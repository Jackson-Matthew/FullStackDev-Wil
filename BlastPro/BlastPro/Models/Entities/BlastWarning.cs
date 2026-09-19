using BlastPro.Models.Enums;

namespace BlastPro.Models.Entities;

public sealed class BlastWarning
{
    public int Id { get; set; }
    public int CalculationResultId { get; set; }
    public WarningSeverity Severity { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }

    public CalculationResult CalculationResult { get; set; } = null!;
}
