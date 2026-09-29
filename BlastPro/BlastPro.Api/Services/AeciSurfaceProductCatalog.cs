using BlastPro.Api.Models.Dtos;

namespace BlastPro.Api.Services;

/// <summary>
/// Surface bulk explosive products transcribed from the supplied AECI Mining
/// Better Blasting Product Brochure, product tables on printed pages 10–21.
/// Density and energy are catalogue properties, not site loading instructions.
/// </summary>
public static class AeciSurfaceProductCatalog
{
    private static AeciProductOptionDto Product(string code, string name, string series,
        decimal? densityMin, decimal? densityMax, decimal energyMin, decimal energyMax,
        int? minimumDiameterMm = null) =>
        new()
        {
            Code = code, Name = name, Series = series,
            Application = series switch
            {
                "S100 / S100 Eco" or "Powergel Eco" => "Surface mining; ground without prevalent heat or reactivity",
                "Powergel X²" => "Hot or reactive surface ground",
                "S300 / S300 Eco" or "S300 heavy ANFO" => "Reactive surface ground",
                "S300 Supreme" => "Highly reactive surface ground",
                "S300 Volcano" => "Geothermal or reactive surface ground",
                _ => "Surface mining"
            },
            DensityMinGramsPerCc = densityMin, DensityMaxGramsPerCc = densityMax,
            EnergyMinMjPerKg = energyMin, EnergyMaxMjPerKg = energyMax,
            MinimumDiameterMillimetres = minimumDiameterMm
        };

    public static IReadOnlyList<AeciProductOptionDto> Products { get; } =
    [
        Product("S100", "S100", "S100 / S100 Eco", 1.00m, 1.25m, 2.05m, 2.20m, 75),
        Product("S100-ECO", "S100 Eco", "S100 / S100 Eco", 1.00m, 1.25m, 2.05m, 2.20m, 75),
        Product("S120", "S120", "S100 / S100 Eco", 1.00m, 1.25m, 2.10m, 2.35m, 75),
        Product("S130", "S130", "S100 / S100 Eco", 1.00m, 1.25m, 2.25m, 2.50m, 100),
        Product("S135", "S135", "S100 / S100 Eco", 1.00m, 1.25m, 2.30m, 2.55m, 100),
        Product("S150", "S150", "S100 / S100 Eco", 1.00m, 1.25m, 2.40m, 2.65m, 229),

        Product("PG-ECO", "Powergel Eco", "Powergel Eco", 1.00m, 1.25m, 2.00m, 2.20m, 75),
        Product("PG-ECO-20", "Powergel Eco-20", "Powergel Eco", 1.00m, 1.25m, 2.20m, 2.40m, 75),
        Product("PG-ECO-30", "Powergel Eco-30", "Powergel Eco", 1.00m, 1.25m, 2.30m, 2.50m, 100),
        Product("PG-ECO-35", "Powergel Eco-35", "Powergel Eco", 1.00m, 1.25m, 2.30m, 2.60m, 100),
        Product("PG-ECO-50", "Powergel Eco-50", "Powergel Eco", 1.00m, 1.25m, 2.50m, 2.70m, 229),

        Product("PG-X2", "Powergel X²", "Powergel X²", 1.20m, 1.25m, 2.23m, 2.30m, 75),
        Product("PG-X2-35", "Powergel X²-35", "Powergel X²", 1.20m, 1.25m, 2.50m, 2.65m, 75),

        Product("S300", "S300", "S300 / S300 Eco", 1.00m, 1.25m, 2.183m, 2.183m, 75),
        Product("S300-ECO", "S300 Eco", "S300 / S300 Eco", 1.00m, 1.25m, 2.183m, 2.183m, 75),
        Product("S320", "S320", "S300 / S300 Eco", 1.00m, 1.25m, 2.381m, 2.381m),
        Product("S330", "S330", "S300 / S300 Eco", 1.00m, 1.25m, 2.474m, 2.474m),
        Product("S335", "S335", "S300 / S300 Eco", 1.00m, 1.25m, 2.544m, 2.544m),

        Product("S350", "S350", "S300 heavy ANFO", 1.00m, 1.25m, 2.639m, 2.639m),
        Product("S354", "S354", "S300 heavy ANFO", 1.35m, 1.35m, 2.834m, 2.834m),
        Product("S357", "S357", "S300 heavy ANFO", 1.30m, 1.30m, 2.780m, 2.780m),
        Product("S360", "S360", "S300 heavy ANFO", 1.25m, 1.25m, 2.727m, 2.727m),
        Product("S364", "S364", "S300 heavy ANFO", 1.20m, 1.20m, 2.687m, 2.687m),
        Product("S371", "S371", "S300 heavy ANFO", 1.10m, 1.10m, 2.570m, 2.570m),
        Product("S374", "S374", "S300 heavy ANFO", 1.05m, 1.05m, 2.502m, 2.502m),
        Product("S380", "S380", "S300 heavy ANFO", 1.00m, 1.00m, 2.474m, 2.474m),

        Product("S300-SUPREME", "S300 Supreme", "S300 Supreme", 1.00m, 1.22m, 2.00m, 2.30m, 75),
        Product("S300-VOLCANO", "S300 Volcano", "S300 Volcano", 1.15m, 1.20m, 2.00m, 2.15m, 80)
    ];

    public static AeciProductOptionDto? Find(string? code) =>
        Products.FirstOrDefault(product => product.Code == code);
}
