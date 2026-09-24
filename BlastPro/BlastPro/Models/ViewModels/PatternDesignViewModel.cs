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
    public decimal VibrationThreshold { get; set; }
    public string? RowVersion { get; set; }
    public List<BlastHoleViewModel> Holes { get; set; } = new();
    public List<ExplosiveProductOptionViewModel> ExplosiveProducts { get; set; } = new();
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
    public decimal? SubdrillMetres { get; set; }
    public decimal? ReceptorDistanceMetres { get; set; }
    public decimal? PpvSiteCoefficient { get; set; }
    public decimal? PpvDecayExponent { get; set; }
    public decimal? FlyrockLaunchSpeedMetresPerSecond { get; set; }
    public decimal? FlyrockLaunchAngleDegrees { get; set; }
    public decimal? FlyrockLaunchHeightMetres { get; set; }
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
    public int? ExplosiveProductId { get; set; }
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

public sealed class InvariantDecimalModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var valueResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueResult == ValueProviderResult.None)
            return Task.CompletedTask;

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueResult);
        var value = valueResult.FirstValue;
        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
            bindingContext.Result = ModelBindingResult.Success(parsed);
        else
            bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, "Enter a valid number.");

        return Task.CompletedTask;
    }
}
