namespace BlastPro.Api.Models.Entities;

public sealed class ExplosiveProduct
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal PricePerKg { get; set; }
    public string CurrencyCode { get; set; } = "ZAR";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Company Company { get; set; } = null!;
    public ICollection<BlastProject> Projects { get; set; } = new List<BlastProject>();
    public ICollection<BlastHole> Holes { get; set; } = new List<BlastHole>();
}
