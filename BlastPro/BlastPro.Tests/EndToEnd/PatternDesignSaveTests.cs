using System.Net;
using BlastPro.Api.Data;
using BlastPro.Api.Models.Entities;
using BlastPro.Api.Models.Enums;
using BlastPro.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace BlastPro.Tests.EndToEnd;

public sealed class PatternDesignSaveTests
{
    // Posts every field the page itself submits, including the dropdowns a browser
    // sends as empty strings when nothing has been chosen yet.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Saving_a_draft_with_unchosen_product_dropdowns_succeeds(bool continueToResults)
    {
        await using var api = new ApiTestHost();
        var user = await api.AddUserAsync();
        int projectId;
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var project = new BlastProject
            {
                CompanyId = user.CompanyId, OwnerId = user.Id, Name = "Browser post",
                SiteLocation = "Test site", BlastType = "Surface", Status = ProjectStatus.Draft
            };
            db.BlastProjects.Add(project);
            await db.SaveChangesAsync();
            projectId = project.Id;
        }

        await using var mvc = new MvcTestHost(api);
        using var browser = mvc.Browser();
        await BrowserForms.SubmitAsync(browser, "/Account/Login", "/Account/Login",
            new Dictionary<string, string> { ["Email"] = user.Email!, ["Password"] = ApiTestHost.Password,
                ["RememberMe"] = "False" });

        var fields = new Dictionary<string, string>
        {
            ["ProjectId"] = projectId.ToString(), ["ProjectName"] = "Browser post", ["Status"] = "Draft",
            ["RowVersion"] = "",
            ["BenchLengthMetres"] = "24", ["BenchWidthMetres"] = "12", ["LayoutRows"] = "2",
            ["LayoutColumns"] = "2", ["PatternType"] = "Rectangular", ["Burden"] = "6", ["Spacing"] = "12",
            ["RockType"] = "Granite", ["DefaultAeciProductCode"] = "", ["ReferenceExplosiveFamily"] = "",
            ["LoadingDensityGramsPerCc"] = "", ["RockDensity"] = "", ["VibrationThreshold"] = "",
            ["VibrationThresholdMode"] = "Manual", ["ReceptorStructureType"] = "Unspecified",
            ["DominantFrequencyHz"] = "", ["TimingOrder"] = "Rows", ["TimingIntervalMilliseconds"] = "",
            ["Calculation.DelayWindowMilliseconds"] = "", ["Calculation.ReceptorDistanceMetres"] = "",
            ["Calculation.PpvSiteCoefficient"] = "", ["Calculation.PpvDecayExponent"] = "",
            ["Calculation.FlyrockLaunchSpeedMetresPerSecond"] = "",
            ["Calculation.FlyrockLaunchAngleDegrees"] = "", ["Calculation.FlyrockLaunchHeightMetres"] = "",
            ["Calculation.ExclusionRadiusMetres"] = "",
            ["Holes[0].Id"] = "", ["Holes[0].Number"] = "1", ["Holes[0].X"] = "0.00", ["Holes[0].Y"] = "0.00",
            ["Holes[0].Depth"] = "10.0", ["Holes[0].DiameterMillimetres"] = "", ["Holes[0].SubdrillMetres"] = "",
            ["Holes[0].ExplosiveProductId"] = "", ["Holes[0].AeciProductCode"] = "",
            ["Holes[0].ProductDensityGramsPerCc"] = "", ["Holes[0].Charge"] = "0.0",
            ["Holes[0].Stemming"] = "0.0", ["Holes[0].Delay"] = "0"
        };
        if (continueToResults) fields["continueToResults"] = "true";

        var response = await BrowserForms.SubmitAsync(browser,
            $"/PatternDesign/Index?projectId={projectId}", "/PatternDesign/SaveDraft", fields);

        var body = response.StatusCode == HttpStatusCode.OK ? await response.Content.ReadAsStringAsync() : "";
        Assert.True(response.StatusCode == HttpStatusCode.Redirect, "Save did not redirect. " + body);
        Assert.Contains(continueToResults ? "Results" : "PatternDesign", response.Headers.Location!.ToString());
    }
}
