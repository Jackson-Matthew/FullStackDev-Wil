
using BlastPro.Api.Models.Entities;
using BlastPro.Api.Services;

namespace BlastPro.Tests.Unit;

public sealed class CalculationServiceTests
{
    [Fact]
    public void Aeci_catalogue_choice_does_not_inherit_legacy_product_price()
    {
        var project = new BlastProject { CompanyId = 1, ExplosiveProductId = 7 };
        var holes = new[] { new BlastHole { AeciProductCode = "S100", ChargeKg = 50 } };
        var products = new[] { new ExplosiveProduct
            { Id = 7, CompanyId = 1, PricePerKg = 100m, CurrencyCode = "ZAR" } };

        var cost = CalculationService.CalculateTotalCost(project, holes, products);

        Assert.Null(cost.Amount);
        Assert.Null(cost.CurrencyCode);
    }

    [Fact]
    public void Volume_uses_each_holes_subdrill_before_the_legacy_common_value()
    {
        var project = new BlastProject { Burden = 2m, Spacing = 3m };
        var holes = new[]
        {
            new BlastHole { Depth = 10m, SubdrillMetres = 1m },
            new BlastHole { Depth = 12m, SubdrillMetres = 2m }
        };

        Assert.Equal(114m, CalculationService.CalculateVolumeCubicMetres(project, holes, 4m));
    }
}
