using BlastPro.Mvc.Models.ViewModels.Projects;
using BlastPro.Mvc.Services.Interfaces;

namespace BlastPro.Mvc.Services;

public static class SavedDraftCalculator
{
    public static async Task<string?> CalculateAsync(IApiClient api, int projectId)
    {
        var saved = await api.GetAsync<PatternDesignViewModel>(
            $"api/projects/{projectId}/pattern-design");
        if (!saved.Success || saved.Data is null)
            return saved.Error ?? "Could not load the saved pattern.";

        var model = saved.Data;
        var missing = new List<string>();
        if (model.Holes.Count == 0) missing.Add("at least one hole");
        if (model.Holes.Any(hole => hole.Charge <= 0 || hole.Stemming <= 0))
            missing.Add("positive charge and stemming for every hole");
        if (model.Calculation.DelayWindowMilliseconds is null or <= 0)
            missing.Add("a site charge grouping window");
        if (model.RockDensity <= 0 || model.Burden <= 0 || model.Spacing <= 0)
            missing.Add("rock density, burden and spacing");
        if (model.VibrationThreshold <= 0 || model.Calculation.ExclusionRadiusMetres is null or <= 0)
            missing.Add("vibration and exclusion limits");
        if (missing.Count > 0)
            return "Complete the saved draft before calculating: " + string.Join("; ", missing) + ".";

        var calculation = await api.PostAsync<object>(
            $"api/projects/{projectId}/calculations",
            new
            {
                DelayWindowMilliseconds = model.Calculation.DelayWindowMilliseconds.GetValueOrDefault(),
                model.Calculation.SubdrillMetres,
                model.Calculation.ReceptorDistanceMetres,
                model.Calculation.PpvSiteCoefficient,
                model.Calculation.PpvDecayExponent,
                model.Calculation.FlyrockLaunchSpeedMetresPerSecond,
                model.Calculation.FlyrockLaunchAngleDegrees,
                model.Calculation.FlyrockLaunchHeightMetres,
                model.Calculation.ExclusionRadiusMetres
            });
        return calculation.Success ? null : calculation.Error ?? "The calculation could not be saved.";
    }
}
