using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BlastPro.Mvc.Models.ViewModels.Projects;

public class PatternDesignViewModel
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft";
    public string RockType { get; set; } = string.Empty;
    [ModelBinder(BinderType = typeof(InvariantDecimalModelBinder))]
    public decimal RockDensity { get; set; }
    [ModelBinder(BinderType = typeof(InvariantDecimalModelBinder))]
    public decimal Burden { get; set; }
    [ModelBinder(BinderType = typeof(InvariantDecimalModelBinder))]
    public decimal Spacing { get; set; }
    [ModelBinder(BinderType = typeof(InvariantDecimalModelBinder))]
    public decimal? BenchLengthMetres { get; set; }
    [ModelBinder(BinderType = typeof(InvariantDecimalModelBinder))]
    public decimal? BenchWidthMetres { get; set; }
    public int? LayoutRows { get; set; }
    public int? LayoutColumns { get; set; }
    public string TimingOrder { get; set; } = "Rows";
    public int? TimingIntervalMilliseconds { get; set; }
    public string PatternType { get; set; } = "Rectangular";
    public string ReferenceExplosiveFamily { get; set; } = "";
    public string DefaultAeciProductCode { get; set; } = "";
    [ModelBinder(BinderType = typeof(InvariantDecimalModelBinder))]
    public decimal? LoadingDensityGramsPerCc { get; set; }
    [ModelBinder(BinderType = typeof(InvariantDecimalModelBinder))]
    public decimal VibrationThreshold { get; set; }
    public string ReceptorStructureType { get; set; } = "Unspecified";
    [ModelBinder(BinderType = typeof(InvariantDecimalModelBinder))]
    public decimal? DominantFrequencyHz { get; set; }
    public string VibrationThresholdMode { get; set; } = "Manual";
    public string? RowVersion { get; set; }
    public List<BlastHoleViewModel> Holes { get; set; } = new();
    public List<ExplosiveProductOptionViewModel> ExplosiveProducts { get; set; } = new();
    public List<AeciProductOptionViewModel> AeciProducts { get; set; } = new();
    public CalculationInputsViewModel Calculation { get; set; } = new();

    public static IReadOnlyList<string> RockTypes { get; } =
    [
        "Granite",
        "Limestone",
        "Sandstone",
        "Shale",
        "Other"
    ];

}

public sealed class CalculationInputsViewModel
{
    public int? DelayWindowMilliseconds { get; set; }
    [ModelBinder(BinderType = typeof(InvariantDecimalModelBinder))]
    public decimal? SubdrillMetres { get; set; }
    [ModelBinder(BinderType = typeof(InvariantDecimalModelBinder))]
    public decimal? ReceptorDistanceMetres { get; set; }
    [ModelBinder(BinderType = typeof(InvariantDecimalModelBinder))]
    public decimal? PpvSiteCoefficient { get; set; }
    [ModelBinder(BinderType = typeof(InvariantDecimalModelBinder))]
    public decimal? PpvDecayExponent { get; set; }
    [ModelBinder(BinderType = typeof(InvariantDecimalModelBinder))]
    public decimal? FlyrockLaunchSpeedMetresPerSecond { get; set; }
    [ModelBinder(BinderType = typeof(InvariantDecimalModelBinder))]
    public decimal? FlyrockLaunchAngleDegrees { get; set; }
    [ModelBinder(BinderType = typeof(InvariantDecimalModelBinder))]
    public decimal? FlyrockLaunchHeightMetres { get; set; }
    [ModelBinder(BinderType = typeof(InvariantDecimalModelBinder))]
    public decimal? ExclusionRadiusMetres { get; set; }
}

public class BlastHoleViewModel
{
    public int? Id { get; set; }
    public int Number { get; set; }
    [ModelBinder(BinderType = typeof(InvariantDecimalModelBinder))]
    public decimal X { get; set; }
    [ModelBinder(BinderType = typeof(InvariantDecimalModelBinder))]
    public decimal Y { get; set; }
    [ModelBinder(BinderType = typeof(InvariantDecimalModelBinder))]
    public decimal Depth { get; set; }
    [ModelBinder(BinderType = typeof(InvariantDecimalModelBinder))]
    public decimal? DiameterMillimetres { get; set; }
    [ModelBinder(BinderType = typeof(InvariantDecimalModelBinder))]
    public decimal? SubdrillMetres { get; set; }
    public int? ExplosiveProductId { get; set; }
    public string AeciProductCode { get; set; } = "";
    [ModelBinder(BinderType = typeof(InvariantDecimalModelBinder))]
    public decimal? ProductDensityGramsPerCc { get; set; }
    [ModelBinder(BinderType = typeof(InvariantDecimalModelBinder))]
    public decimal Charge { get; set; }
    [ModelBinder(BinderType = typeof(InvariantDecimalModelBinder))]
    public decimal Stemming { get; set; }
    public int Delay { get; set; }
}

public class ExplosiveProductOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class AeciProductOptionViewModel
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Series { get; set; } = "";
    public string Application { get; set; } = "";
    public decimal? DensityMinGramsPerCc { get; set; }
    public decimal? DensityMaxGramsPerCc { get; set; }
    public decimal EnergyMinMjPerKg { get; set; }
    public decimal EnergyMaxMjPerKg { get; set; }
    public int? MinimumDiameterMillimetres { get; set; }
}

public sealed class InvariantDecimalModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var valueResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueResult == ValueProviderResult.None)
            return Task.CompletedTask;

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueResult);
        var value = valueResult.FirstValue;
        if (string.IsNullOrWhiteSpace(value))
        {
            bindingContext.Result = ModelBindingResult.Success(
                Nullable.GetUnderlyingType(bindingContext.ModelMetadata.ModelType) == typeof(decimal)
                    ? null : 0m);
            return Task.CompletedTask;
        }

        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
            bindingContext.Result = ModelBindingResult.Success(parsed);
        else
            bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, "Enter a valid number.");

        return Task.CompletedTask;
    }
}
