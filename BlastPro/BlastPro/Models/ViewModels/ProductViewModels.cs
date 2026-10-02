using System.ComponentModel.DataAnnotations;

namespace BlastPro.Mvc.Models.ViewModels;

public sealed class ProductViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? AeciProductCode { get; set; }
    public decimal PricePerKg { get; set; }
    public string CurrencyCode { get; set; } = "ZAR";
    public bool IsActive { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class ProductFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(150, MinimumLength = 2)]
    public string Name { get; set; } = "";

    [Display(Name = "AECI catalogue product")]
    public string? AeciProductCode { get; set; }

    [Display(Name = "Price per kilogram (ZAR)")]
    [Range(typeof(decimal), "0.01", "1000000", ParseLimitsInInvariantCulture = true)]
    public decimal PricePerKg { get; set; }

    public bool IsActive { get; set; } = true;
    public List<ProductCatalogViewModel> Catalog { get; set; } = [];
}

public sealed class ProductCatalogViewModel
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
}
