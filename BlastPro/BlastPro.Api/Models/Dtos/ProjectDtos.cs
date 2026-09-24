namespace BlastPro.Api.Models.Dtos;

public class ProjectSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SiteLocation { get; set; } = string.Empty;
    public string BlastType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

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

public class CreateProjectDto
{
    public string Name { get; set; } = string.Empty;
    public string SiteLocation { get; set; } = string.Empty;
    public string BlastType { get; set; } = string.Empty;
}

public class UpdateProjectDto
{
    public string? Name { get; set; }
    public string? SiteLocation { get; set; }
    public string? BlastType { get; set; }
    public string? RockType { get; set; }
    public decimal? RockDensity { get; set; }
    public decimal? Burden { get; set; }
    public decimal? Spacing { get; set; }
    public decimal? VibrationThreshold { get; set; }
}

public class PatternDesignDto
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string RockType { get; set; } = string.Empty;
    public decimal RockDensity { get; set; }
    public decimal Burden { get; set; }
    public decimal Spacing { get; set; }
    public decimal VibrationThreshold { get; set; }
    public string RowVersion { get; set; } = string.Empty;
    public List<PatternHoleDto> Holes { get; set; } = new();
    public List<ExplosiveProductOptionDto> ExplosiveProducts { get; set; } = new();
}

public class PatternHoleDto
{
    public int? Id { get; set; }
    public int Number { get; set; }
    public decimal X { get; set; }
    public decimal Y { get; set; }
    public decimal Depth { get; set; }
    public int? ExplosiveProductId { get; set; }
    public decimal Charge { get; set; }
    public decimal Stemming { get; set; }
    public int Delay { get; set; }
}

public class ExplosiveProductOptionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class SavePatternDesignDto
{
    public string? RockType { get; set; }
    public decimal RockDensity { get; set; }
    public decimal Burden { get; set; }
    public decimal Spacing { get; set; }
    public decimal VibrationThreshold { get; set; }
    public string? RowVersion { get; set; }
    public List<PatternHoleDto> Holes { get; set; } = new();
}
