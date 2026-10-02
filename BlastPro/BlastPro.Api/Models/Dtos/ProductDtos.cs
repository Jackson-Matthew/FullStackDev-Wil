using System.ComponentModel.DataAnnotations;

namespace BlastPro.Api.Models.Dtos;

public sealed record ProductDto(int Id, string Name, string? AeciProductCode, decimal PricePerKg, string CurrencyCode,
    bool IsActive, DateTime UpdatedAtUtc);

public sealed record ProductCatalogDto(string Code, string Name);

public sealed class SaveProductRequest
{
    [Required, StringLength(150, MinimumLength = 2)]
    public string Name { get; set; } = "";

    [StringLength(40)]
    public string? AeciProductCode { get; set; }

    [Range(typeof(decimal), "0.01", "1000000", ParseLimitsInInvariantCulture = true)]
    public decimal PricePerKg { get; set; }

    public bool IsActive { get; set; } = true;
}
