using BlastPro.Api.Models.Entities;
using BlastPro.Api.Models.Enums;

namespace BlastPro.Api.Services;

/// <summary>
/// Pure calculations for the Task 2 pattern-design results page. This class does not
/// read or write the database. The caller supplies a project, its holes, product prices,
/// and any site-specific parameters, then decides whether and how to save the result.
/// All lengths are metres, masses are kilograms, rock density is tonnes per cubic metre,
/// and monetary values use the selected products' common currency.
/// </summary>
public static class CalculationService
{
    private const double GravityMetresPerSecondSquared = 9.80665;

    public static CalculationSummary CalculateAll(
        BlastProject project,
        IEnumerable<BlastHole> holes,
        IEnumerable<ExplosiveProduct> products,
        CalculationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(holes);
        ArgumentNullException.ThrowIfNull(products);
        ArgumentNullException.ThrowIfNull(settings);

        if (settings.DelayWindowMilliseconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(settings), "The delay window must be greater than zero milliseconds.");
        if (project.VibrationThreshold is <= 0 || settings.ExclusionRadiusMetres is <= 0)
            throw new ArgumentOutOfRangeException(nameof(settings), "Entered vibration and exclusion limits must be positive.");

        var holeList = holes.ToArray();
        ValidateHoles(project, holeList);
        var productList = products.ToArray();
        ValidateProducts(project, productList);

        var warnings = new List<CalculationWarning>();
        if (holeList.Length == 0)
            warnings.Add(new("NO_HOLES", WarningSeverity.Critical, "Add at least one hole before using these results."));

        var totalExplosiveKg = CalculateTotalExplosiveKg(holeList);
        var maxChargeKg = CalculateMaxChargePerDelayKg(holeList, settings.DelayWindowMilliseconds);
        var volume = CalculateVolumeCubicMetres(project, holeList, settings.SubdrillMetres);
        if (volume is null)
            warnings.Add(new("VOLUME_UNAVAILABLE", WarningSeverity.Warning,
                "Volume needs burden, spacing, and the subdrill length in metres."));

        var tonnage = CalculateTonnageTonnes(volume, project.RockDensity);
        if (tonnage is null)
            warnings.Add(new("TONNAGE_UNAVAILABLE", WarningSeverity.Warning,
                "Tonnage needs calculated volume and rock density in tonnes per cubic metre."));

        var powderFactor = CalculatePowderFactorKgPerTonne(totalExplosiveKg, tonnage);
        if (powderFactor is null)
            warnings.Add(new("POWDER_FACTOR_UNAVAILABLE", WarningSeverity.Warning,
                "Powder factor needs a positive estimated tonnage."));

        var cost = CalculateTotalCost(project, holeList, productList);
        if (cost.Amount is null)
            warnings.Add(new("COST_UNAVAILABLE", WarningSeverity.Warning,
                "Every hole needs a priced explosive product, and all prices must use one currency."));

        var ppv = CalculatePpvMmPerSecond(maxChargeKg, settings);
        if (ppv is null)
            warnings.Add(new("PPV_UNAVAILABLE", WarningSeverity.Warning,
                "PPV needs a receptor distance and site-calibrated coefficient and exponent."));
        else if (project.VibrationThreshold is null)
            warnings.Add(new("PPV_LIMIT_UNAVAILABLE", WarningSeverity.Warning,
                "A site-approved vibration threshold in mm/s is needed to assess predicted PPV."));
        else if (ppv > project.VibrationThreshold)
            warnings.Add(new("PPV_LIMIT_EXCEEDED", WarningSeverity.Critical,
                "Predicted PPV exceeds the project's entered vibration threshold; a qualified specialist must review the design."));

        var flyrock = holeList.Length > 0 && totalExplosiveKg > 0
            ? EstimateIdealizedFlyrockRangeMetres(settings)
            : null;
        if (flyrock is null)
            warnings.Add(new("FLYROCK_UNAVAILABLE", WarningSeverity.Warning,
                "Flyrock screening needs an approved launch-speed estimate, launch angle, and launch height."));
        else
        {
            warnings.Add(new("FLYROCK_SCREENING_ONLY", WarningSeverity.Warning,
                "Flyrock is an idealized trajectory estimate, not a validated blast-area or exclusion distance."));
            if (settings.ExclusionRadiusMetres is null)
                warnings.Add(new("FLYROCK_LIMIT_UNAVAILABLE", WarningSeverity.Warning,
                    "An approved exclusion radius is needed to compare with the flyrock screening estimate."));
            if (settings.ExclusionRadiusMetres is > 0 && flyrock > settings.ExclusionRadiusMetres)
                warnings.Add(new("FLYROCK_EXCLUSION_EXCEEDED", WarningSeverity.Critical,
                    "The idealized flyrock estimate exceeds the entered exclusion radius; a qualified specialist must review it."));
        }

        return new CalculationSummary(
            holeList.Length,
            totalExplosiveKg,
            CalculateTotalDrillingMetres(holeList),
            volume,
            tonnage,
            cost.Amount,
            cost.CurrencyCode,
            maxChargeKg,
            powderFactor,
            ppv,
            flyrock,
            warnings.AsReadOnly());
    }

    public static decimal CalculateTotalDrillingMetres(IEnumerable<BlastHole> holes)
    {
        ArgumentNullException.ThrowIfNull(holes);
        decimal total = 0;
        foreach (var hole in holes)
        {
            if (hole.Depth <= 0)
                throw new ArgumentException("Hole depth must be positive.", nameof(holes));
            total += hole.Depth;
        }
        return total;
    }

    public static decimal CalculateTotalExplosiveKg(IEnumerable<BlastHole> holes)
    {
        ArgumentNullException.ThrowIfNull(holes);
        decimal total = 0;
        foreach (var hole in holes)
        {
            if (hole.ChargeKg < 0)
                throw new ArgumentException("Hole charge must be non-negative.", nameof(holes));
            total += hole.ChargeKg;
        }
        return total;
    }

    /// <summary>
    /// Finds the greatest charge inside any moving delay window. The window is supplied
    /// by the caller because its length must follow the site's approved timing rule.
    /// Source for charge-per-delay practice: https://www.osmre.gov/sites/default/files/pdfs/directive315.pdf
    /// </summary>
    public static decimal CalculateMaxChargePerDelayKg(IEnumerable<BlastHole> holes, int delayWindowMilliseconds)
    {
        ArgumentNullException.ThrowIfNull(holes);
        if (delayWindowMilliseconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(delayWindowMilliseconds));

        var ordered = holes.OrderBy(hole => hole.DelayMilliseconds).ToArray();
        if (ordered.Any(hole => hole.DelayMilliseconds < 0 || hole.ChargeKg < 0))
            throw new ArgumentException("Hole charges and delays must be non-negative.", nameof(holes));
        decimal current = 0;
        decimal maximum = 0;
        var left = 0;

        for (var right = 0; right < ordered.Length; right++)
        {
            current += ordered[right].ChargeKg;
            while ((long)ordered[right].DelayMilliseconds - ordered[left].DelayMilliseconds >= delayWindowMilliseconds)
                current -= ordered[left++].ChargeKg;
            maximum = Math.Max(maximum, current);
        }

        return maximum;
    }

    /// <summary>
    /// Burden x spacing x bench height per hole, where bench height is drilled depth
    /// minus the caller-supplied subdrill. Returns null rather than assuming zero subdrill.
    /// Source: https://www.osmre.gov/sites/default/files/inline-files/Module3_0.pdf
    /// </summary>
    public static decimal? CalculateVolumeCubicMetres(
        BlastProject project, IEnumerable<BlastHole> holes, decimal? subdrillMetres)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(holes);
        if (subdrillMetres is < 0)
            throw new ArgumentOutOfRangeException(nameof(subdrillMetres));
        if (project.Burden is null || project.Spacing is null || subdrillMetres is null)
            return null;
        if (project.Burden <= 0 || project.Spacing <= 0)
            throw new ArgumentOutOfRangeException(nameof(project), "Burden and spacing must be positive metres.");

        decimal total = 0;
        foreach (var hole in holes)
        {
            if (hole.Depth <= 0)
                throw new ArgumentException("Hole depth must be positive.", nameof(holes));
            var benchHeight = hole.Depth - subdrillMetres.Value;
            if (benchHeight <= 0)
                throw new ArgumentOutOfRangeException(nameof(subdrillMetres),
                    "Subdrill must be less than every hole's drilled depth.");
            total += project.Burden.Value * project.Spacing.Value * benchHeight;
        }
        return total;
    }

    public static decimal? CalculateTonnageTonnes(decimal? volumeCubicMetres, decimal? rockDensityTonnesPerCubicMetre)
    {
        if (volumeCubicMetres is < 0 || rockDensityTonnesPerCubicMetre is <= 0)
            throw new ArgumentOutOfRangeException(nameof(rockDensityTonnesPerCubicMetre));
        return volumeCubicMetres * rockDensityTonnesPerCubicMetre;
    }

    public static decimal? CalculatePowderFactorKgPerTonne(decimal totalExplosiveKg, decimal? tonnageTonnes)
    {
        if (totalExplosiveKg < 0 || tonnageTonnes is < 0)
            throw new ArgumentOutOfRangeException(nameof(totalExplosiveKg));
        return tonnageTonnes is > 0 ? totalExplosiveKg / tonnageTonnes : null;
    }

    public static CalculationCost CalculateTotalCost(
        BlastProject project, IEnumerable<BlastHole> holes, IEnumerable<ExplosiveProduct> products)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(holes);
        ArgumentNullException.ThrowIfNull(products);

        var priceById = products.ToDictionary(product => product.Id);
        decimal total = 0;
        string? currency = null;

        foreach (var hole in holes)
        {
            if (hole.ChargeKg < 0)
                throw new ArgumentException("Hole charge must be non-negative.", nameof(holes));
            var productId = hole.ExplosiveProductId ?? project.ExplosiveProductId;
            if (productId is null || !priceById.TryGetValue(productId.Value, out var product))
                return new(null, null);
            if (product.PricePerKg < 0 || product.CompanyId != project.CompanyId)
                throw new ArgumentException("A product has an invalid price or belongs to another company.", nameof(products));
            if (string.IsNullOrWhiteSpace(product.CurrencyCode))
                return new(null, null);
            if (currency is not null && !string.Equals(currency, product.CurrencyCode, StringComparison.OrdinalIgnoreCase))
                return new(null, null);
            currency = product.CurrencyCode.ToUpperInvariant();
            total += hole.ChargeKg * product.PricePerKg;
        }

        return new(total, currency);
    }

    /// <summary>
    /// Site-calibrated scaled-distance relationship: PPV = K x (D/sqrt(W))^-n.
    /// K must be calibrated for D in metres, W in kilograms and PPV in mm/s;
    /// no universal coefficient is supplied here.
    /// Source: https://www.osmre.gov/sites/default/files/pdfs/directive315.pdf
    /// </summary>
    public static decimal? CalculatePpvMmPerSecond(decimal maxChargePerDelayKg, CalculationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (maxChargePerDelayKg < 0)
            throw new ArgumentOutOfRangeException(nameof(maxChargePerDelayKg));
        if (maxChargePerDelayKg == 0 || settings.ReceptorDistanceMetres is null ||
            settings.PpvSiteCoefficient is null || settings.PpvDecayExponent is null)
            return null;
        if (settings.ReceptorDistanceMetres <= 0 || settings.PpvSiteCoefficient <= 0 || settings.PpvDecayExponent <= 0)
            throw new ArgumentOutOfRangeException(nameof(settings), "PPV distance, coefficient and decay exponent must be positive.");

        var scaledDistance = (double)settings.ReceptorDistanceMetres.Value /
                             Math.Sqrt((double)maxChargePerDelayKg);
        var value = (double)settings.PpvSiteCoefficient.Value *
                    Math.Pow(scaledDistance, -(double)settings.PpvDecayExponent.Value);
        if (!double.IsFinite(value) || value > (double)decimal.MaxValue)
            throw new OverflowException("The PPV estimate is outside the supported numeric range.");
        return (decimal)value;
    }

    /// <summary>
    /// Idealized projectile trajectory from an approved launch-speed estimate. This
    /// ignores air drag, rock shape and uncertain field conditions and is not a safe
    /// exclusion-radius prediction. Returns null unless all inputs are supplied.
    /// Ballistic basis: https://stacks.cdc.gov/view/cdc/206583/cdc_206583_DS1.pdf
    /// Limitations: https://mhsc.org.za/wp-content/uploads/2024/06/SAIMM-Flyrock-P1-v122n12p725-1.pdf
    /// </summary>
    public static decimal? EstimateIdealizedFlyrockRangeMetres(CalculationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (settings.FlyrockLaunchSpeedMetresPerSecond is null ||
            settings.FlyrockLaunchAngleDegrees is null || settings.FlyrockLaunchHeightMetres is null)
            return null;
        if (settings.FlyrockLaunchSpeedMetresPerSecond <= 0 ||
            settings.FlyrockLaunchAngleDegrees is < 0 or > 90 || settings.FlyrockLaunchHeightMetres < 0)
            throw new ArgumentOutOfRangeException(nameof(settings), "Flyrock launch inputs are outside their valid ranges.");

        var speed = (double)settings.FlyrockLaunchSpeedMetresPerSecond.Value;
        var angle = (double)settings.FlyrockLaunchAngleDegrees.Value * Math.PI / 180;
        var height = (double)settings.FlyrockLaunchHeightMetres.Value;
        var verticalSpeed = speed * Math.Sin(angle);
        var time = (verticalSpeed + Math.Sqrt(verticalSpeed * verticalSpeed +
                    2 * GravityMetresPerSecondSquared * height)) / GravityMetresPerSecondSquared;
        var range = speed * Math.Cos(angle) * time;
        if (!double.IsFinite(range) || range > (double)decimal.MaxValue)
            throw new OverflowException("The flyrock estimate is outside the supported numeric range.");
        return (decimal)range;
    }

    private static void ValidateHoles(BlastProject project, IReadOnlyCollection<BlastHole> holes)
    {
        var holeNumbers = new HashSet<int>();
        foreach (var hole in holes)
        {
            if (hole.Depth <= 0 || hole.ChargeKg < 0 || hole.StemmingMetres < 0 ||
                hole.StemmingMetres > hole.Depth || hole.DelayMilliseconds < 0 ||
                hole.HoleNumber <= 0 || !holeNumbers.Add(hole.HoleNumber))
                throw new ArgumentException("Holes must have unique positive numbers, valid lengths, charges and delays.", nameof(holes));
            if (project.Id > 0 && hole.BlastProjectId > 0 && hole.BlastProjectId != project.Id)
                throw new ArgumentException("A hole belongs to a different project.", nameof(holes));
        }
    }

    private static void ValidateProducts(BlastProject project, IEnumerable<ExplosiveProduct> products)
    {
        foreach (var product in products)
        {
            if (product.CompanyId != project.CompanyId || product.PricePerKg < 0)
                throw new ArgumentException("Products must belong to the project company and have non-negative prices.", nameof(products));
        }
    }
}

/// <summary>Extra inputs not stored on the Task 2 project and hole entities.</summary>
public sealed class CalculationSettings
{
    /// <summary>Site-approved window used to group charges by delay, in milliseconds.</summary>
    public int DelayWindowMilliseconds { get; init; }
    /// <summary>Common subdrill for these holes, in metres; required for volume.</summary>
    public decimal? SubdrillMetres { get; init; }
    /// <summary>Distance from blast to receptor, in metres.</summary>
    public decimal? ReceptorDistanceMetres { get; init; }
    /// <summary>Site-calibrated K for metric scaled distance and PPV in mm/s.</summary>
    public decimal? PpvSiteCoefficient { get; init; }
    public decimal? PpvDecayExponent { get; init; }
    /// <summary>Launch speed supplied by a site-approved flyrock model, in m/s.</summary>
    public decimal? FlyrockLaunchSpeedMetresPerSecond { get; init; }
    public decimal? FlyrockLaunchAngleDegrees { get; init; }
    public decimal? FlyrockLaunchHeightMetres { get; init; }
    public decimal? ExclusionRadiusMetres { get; init; }
}

public sealed record CalculationWarning(string Code, WarningSeverity Severity, string Message);
public sealed record CalculationCost(decimal? Amount, string? CurrencyCode);
public sealed record CalculationSummary(
    int TotalHoles,
    decimal TotalExplosiveKg,
    decimal TotalDrillingMetres,
    decimal? EstimatedVolumeCubicMetres,
    decimal? EstimatedTonnageTonnes,
    decimal? TotalCost,
    string? CurrencyCode,
    decimal MaxChargePerDelayKg,
    decimal? PowderFactorKgPerTonne,
    decimal? PredictedPpvMmPerSecond,
    decimal? IdealizedFlyrockRangeMetres,
    IReadOnlyList<CalculationWarning> Warnings);
