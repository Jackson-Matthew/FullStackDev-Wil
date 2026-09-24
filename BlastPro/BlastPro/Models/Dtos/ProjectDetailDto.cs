namespace BlastPro.Mvc.Models.Dtos;

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