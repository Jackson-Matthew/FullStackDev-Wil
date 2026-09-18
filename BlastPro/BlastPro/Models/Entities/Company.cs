namespace BlastPro.Models.Entities;

public sealed class Company
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? RegistrationNumber { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    public ICollection<BlastProject> Projects { get; set; } = new List<BlastProject>();
    public ICollection<ExplosiveProduct> ExplosiveProducts { get; set; } = new List<ExplosiveProduct>();
}
