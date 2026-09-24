using System.Net;
using BlastPro.Api.Data;
using BlastPro.Api.Models.Entities;
using BlastPro.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BlastPro.Tests.EndToEnd;

public sealed class ResultsPageTests
{
    [Fact]
    public async Task Calculation_form_saves_real_results_and_page_opens_history()
    {
        await using var api = new ApiTestHost();
        var user = await api.AddUserAsync();
        var (projectId, holeId) = await SeedProject(api, user);
        await using var mvc = new MvcTestHost(api);
        using var browser = mvc.Browser();
        await Login(browser, user.Email!);

        var empty = await browser.GetAsync($"/Results/Index?projectId={projectId}");
        Assert.Equal(HttpStatusCode.OK, empty.StatusCode);
        Assert.Contains("No results yet", await empty.Content.ReadAsStringAsync());

        var pattern = await browser.GetAsync($"/PatternDesign/Index?projectId={projectId}");
        var patternHtml = await pattern.Content.ReadAsStringAsync();
        Assert.Contains("Calculation.DelayWindowMilliseconds", patternHtml);
        Assert.Contains("Calculation.ExclusionRadiusMetres", patternHtml);

        var first = await BrowserForms.SubmitAsync(browser,
            $"/PatternDesign/Index?projectId={projectId}", "/Projects/CalculatePhysics",
            CalculationFields(projectId, holeId));
        Assert.Equal(HttpStatusCode.Redirect, first.StatusCode);
        Assert.Contains("Results", first.Headers.Location!.ToString());

        var page = await browser.GetAsync($"/Results/Index?projectId={projectId}");
        var html = await page.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Contains("108.00", html);
        Assert.Contains("Estimated Volume", html);
        Assert.Contains("Not available", html);
        Assert.Contains("kg/t", html);
        Assert.Contains("Predicted PPV", html);
        Assert.Contains("Idealized Flyrock Range", html);
        Assert.DoesNotContain("Test 1 Pro", html);
        Assert.DoesNotContain("Save Results", html);

        var second = await BrowserForms.SubmitAsync(browser,
            $"/PatternDesign/Index?projectId={projectId}", "/Projects/CalculatePhysics",
            CalculationFields(projectId, holeId));
        Assert.Equal(HttpStatusCode.Redirect, second.StatusCode);

        int firstResultId;
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            firstResultId = (await db.CalculationResults
                .Where(r => r.BlastProjectId == projectId)
                .OrderBy(r => r.Id).FirstAsync()).Id;
        }
        var current = await browser.GetAsync($"/Results/Index?projectId={projectId}");
        var currentHtml = await current.Content.ReadAsStringAsync();
        Assert.Contains("Previous Calculations", currentHtml);
        Assert.Contains($"resultId={firstResultId}", currentHtml);

        var historical = await browser.GetAsync($"/Results/Index?projectId={projectId}&resultId={firstResultId}");
        var historicalHtml = await historical.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, historical.StatusCode);
        Assert.Contains("Previous Calculation", historicalHtml);
        Assert.Contains("View latest result", historicalHtml);

        var changedFields = CalculationFields(projectId, holeId);
        changedFields["Holes[0].Charge"] = "6";
        var savedDraft = await BrowserForms.SubmitAsync(browser,
            $"/PatternDesign/Index?projectId={projectId}", "/PatternDesign/SaveDraft", changedFields);
        Assert.Equal(HttpStatusCode.OK, savedDraft.StatusCode);
        var outdated = await browser.GetAsync($"/Results/Index?projectId={projectId}");
        Assert.Contains("Outdated Calculation", await outdated.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Invalid_site_input_is_shown_on_pattern_page_without_saving_result()
    {
        await using var api = new ApiTestHost();
        var user = await api.AddUserAsync();
        var (projectId, holeId) = await SeedProject(api, user);
        await using var mvc = new MvcTestHost(api);
        using var browser = mvc.Browser();
        await Login(browser, user.Email!);

        var fields = CalculationFields(projectId, holeId);
        fields["Calculation.SubdrillMetres"] = "-1";
        var response = await BrowserForms.SubmitAsync(browser,
            $"/PatternDesign/Index?projectId={projectId}", "/Projects/CalculatePhysics", fields);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Subdrill cannot be negative", html);
        Assert.Contains("Test explosive", html);
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Empty(await db.CalculationResults.Where(r => r.BlastProjectId == projectId).ToListAsync());
    }

    private static async Task Login(HttpClient browser, string email) =>
        Assert.Equal(HttpStatusCode.Redirect, (await BrowserForms.SubmitAsync(browser,
            "/Account/Login", "/Account/Login", new Dictionary<string, string>
            {
                ["Email"] = email,
                ["Password"] = ApiTestHost.Password,
                ["RememberMe"] = "False"
            })).StatusCode);

    private static Dictionary<string, string> CalculationFields(int projectId, int holeId) => new()
    {
        ["ProjectId"] = projectId.ToString(),
        ["ProjectName"] = "Results Project",
        ["Status"] = "Draft",
        ["RowVersion"] = string.Empty,
        ["RockType"] = "Granite",
        ["RockDensity"] = "2.5",
        ["Burden"] = "3",
        ["Spacing"] = "4",
        ["VibrationThreshold"] = "10",
        ["Holes[0].Id"] = holeId.ToString(),
        ["Holes[0].Number"] = "1",
        ["Holes[0].X"] = "0",
        ["Holes[0].Y"] = "0",
        ["Holes[0].Depth"] = "10",
        ["Holes[0].ExplosiveProductId"] = string.Empty,
        ["Holes[0].Charge"] = "5",
        ["Holes[0].Stemming"] = "2",
        ["Holes[0].Delay"] = "25",
        ["Calculation.DelayWindowMilliseconds"] = "50",
        ["Calculation.SubdrillMetres"] = "1"
    };

    private static async Task<(int ProjectId, int HoleId)> SeedProject(ApiTestHost api, ApplicationUser user)
    {
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var product = new ExplosiveProduct
        {
            CompanyId = user.CompanyId, Name = "Test explosive", PricePerKg = 10m,
            CurrencyCode = "ZAR", IsActive = true
        };
        db.ExplosiveProducts.Add(product);
        var project = new BlastProject
        {
            CompanyId = user.CompanyId, OwnerId = user.Id,
            Name = "Results Project", SiteLocation = "Test site", BlastType = "Surface",
            RockType = "Granite", RockDensity = 2.5m, Burden = 3m, Spacing = 4m,
            VibrationThreshold = 10m
        };
        db.BlastProjects.Add(project);
        await db.SaveChangesAsync();
        var hole = new BlastHole
        {
            BlastProjectId = project.Id, HoleNumber = 1, Depth = 10m,
            ChargeKg = 5m, StemmingMetres = 2m, DelayMilliseconds = 25
        };
        db.BlastHoles.Add(hole);
        await db.SaveChangesAsync();
        return (project.Id, hole.Id);
    }
}
