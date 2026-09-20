using Microsoft.AspNetCore.Identity;

namespace BlastPro.Api.Models.Entities;

public sealed class ApplicationUser : IdentityUser
{
    public int CompanyId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? NickName { get; set; }
    public string? Gender { get; set; }
    public string? Country { get; set; }
    public string? TimeZoneId { get; set; }
    public string? CertificationId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Company Company { get; set; } = null!;
    public ICollection<BlastProject> OwnedProjects { get; set; } = new List<BlastProject>();
    public ICollection<CalculationResult> CalculationResults { get; set; } = new List<CalculationResult>();
}
