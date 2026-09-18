namespace BlastPro.Models.Entities;

public sealed class BlastHole
{
    public int Id { get; set; }
    public int BlastProjectId { get; set; }
    public int? ExplosiveProductId { get; set; }
    public int HoleNumber { get; set; }
    public decimal XCoordinate { get; set; }
    public decimal YCoordinate { get; set; }
    public decimal Depth { get; set; }
    public decimal ChargeKg { get; set; }
    public decimal StemmingMetres { get; set; }
    public int DelayMilliseconds { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public BlastProject BlastProject { get; set; } = null!;
    public ExplosiveProduct? ExplosiveProduct { get; set; }
}
