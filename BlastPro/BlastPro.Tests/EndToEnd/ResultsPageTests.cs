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

        var draft = await BrowserForms.SubmitAsync(browser,
            $"/PatternDesign/Index?projectId={projectId}", "/PatternDesign/SaveDraft",
            CalculationFields(projectId, holeId));
        Assert.Equal(HttpStatusCode.Redirect, draft.StatusCode);
        var first = await BrowserForms.SubmitAsync(browser,
            $"/Results/Index?projectId={projectId}", "/Projects/CalculatePhysics",
            new Dictionary<string, string> { ["projectId"] = projectId.ToString() });
        Assert.Equal(HttpStatusCode.Redirect, first.StatusCode);
        Assert.Contains("Results", first.Headers.Location!.ToString());
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var savedProject = await db.BlastProjects.SingleAsync(p => p.Id == projectId);
            Assert.Equal(3, savedProject.LayoutRows);
            Assert.Equal(5, savedProject.LayoutColumns);
            Assert.Equal("Rows", savedProject.TimingOrder);
            Assert.Equal(25, savedProject.TimingIntervalMilliseconds);
            Assert.Equal(BlastPro.Api.Models.Enums.ProjectStatus.Calculated, savedProject.Status);
            Assert.NotNull((await db.CalculationResults.SingleAsync(r => r.BlastProjectId == projectId)).PatternSnapshotJson);
        }

        var page = await browser.GetAsync($"/Results/Index?projectId={projectId}");
        var html = await page.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Contains("102.00", html);
        Assert.Contains("255.00", html);
        Assert.Contains("0.23 mm/s", html);
        Assert.Contains("40.79 m", html);
        Assert.Contains("Estimated Volume", html);
        Assert.Contains("Not available", html);
        Assert.Contains("kg/t", html);
        Assert.Contains("Predicted PPV", html);
        Assert.Contains("Idealized Flyrock Range", html);
        Assert.DoesNotContain("Every hole needs a priced explosive product", html);
        Assert.Contains("Flyrock is an idealized trajectory estimate", html);
        Assert.Contains("severity-warning", html);
        Assert.DoesNotContain("Test 1 Pro", html);
        Assert.DoesNotContain("Save Results", html);
        var report = await browser.GetAsync($"/Reports/Preview?projectId={projectId}");
        var reportHtml = await report.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, report.StatusCode);
        Assert.Contains("Calculation report #", reportHtml);
        Assert.Contains("Saved hole plan and schedule", reportHtml);
        Assert.Contains("report-hole-plan", reportHtml);
        Assert.Contains("S300 Volcano", reportHtml);
        Assert.Contains("Density (g/cm³)", reportHtml);
        Assert.Contains("Download PDF", reportHtml);
        Assert.Contains("Test site", reportHtml);
        var printable = await browser.GetAsync($"/Reports/Print?projectId={projectId}");
        Assert.Equal(HttpStatusCode.OK, printable.StatusCode);
        Assert.Contains("report-hole-plan", await printable.Content.ReadAsStringAsync());
        var pdf = await browser.GetAsync($"/Reports/DownloadPdf?projectId={projectId}");
        Assert.Equal(HttpStatusCode.OK, pdf.StatusCode);
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType?.MediaType);
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString((await pdf.Content.ReadAsByteArrayAsync()).AsSpan(0, 4)));

        var unchangedDraft = await BrowserForms.SubmitAsync(browser,
            $"/PatternDesign/Index?projectId={projectId}", "/PatternDesign/SaveDraft",
            CalculationFields(projectId, holeId));
        Assert.Equal(HttpStatusCode.Redirect, unchangedDraft.StatusCode);
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(BlastPro.Api.Models.Enums.ProjectStatus.Calculated,
                (await db.BlastProjects.SingleAsync(p => p.Id == projectId)).Status);
            Assert.True(await db.CalculationResults.AnyAsync(r => r.BlastProjectId == projectId && r.IsCurrent));
        }

        var second = await BrowserForms.SubmitAsync(browser,
            $"/Results/Index?projectId={projectId}", "/Projects/CalculatePhysics",
            new Dictionary<string, string> { ["projectId"] = projectId.ToString() });
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
        var historicalReport = await browser.GetAsync($"/Reports/Preview?projectId={projectId}&resultId={firstResultId}");
        var historicalReportHtml = await historicalReport.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, historicalReport.StatusCode);
        Assert.Contains("report-hole-plan", historicalReportHtml);
        Assert.Contains("S300 Volcano", historicalReportHtml);

        var changedFields = CalculationFields(projectId, holeId);
        changedFields["Holes[0].Charge"] = "6";
        var savedDraft = await BrowserForms.SubmitAsync(browser,
            $"/PatternDesign/Index?projectId={projectId}", "/PatternDesign/SaveDraft", changedFields);
        Assert.Equal(HttpStatusCode.Redirect, savedDraft.StatusCode);
        var outdated = await browser.GetAsync($"/Results/Index?projectId={projectId}");
        Assert.Contains("Outdated Calculation", await outdated.Content.ReadAsStringAsync());
        var oldReport = await browser.GetAsync($"/Reports/Preview?projectId={projectId}");
        var oldReportHtml = await oldReport.Content.ReadAsStringAsync();
        Assert.Contains("Outdated result", oldReportHtml);
        Assert.Contains("saved design for this result", oldReportHtml);
        Assert.Contains("report-hole-plan", oldReportHtml);
        Assert.Contains("S300 Volcano", oldReportHtml);
        Assert.Contains("<td>5</td>", oldReportHtml);
    }

    [Fact]
    public async Task Incomplete_saved_draft_is_explained_on_results_without_saving_result()
    {
        await using var api = new ApiTestHost();
        var user = await api.AddUserAsync();
        var (projectId, holeId) = await SeedProject(api, user);
        await using var mvc = new MvcTestHost(api);
        using var browser = mvc.Browser();
        await Login(browser, user.Email!);

        var response = await BrowserForms.SubmitAsync(browser,
            $"/Results/Index?projectId={projectId}", "/Projects/CalculatePhysics",
            new Dictionary<string, string> { ["projectId"] = projectId.ToString() });
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var page = await browser.GetAsync(response.Headers.Location!);
        var html = await page.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Contains("Complete the saved draft before calculating", html);
        Assert.Contains("charge grouping window", html);
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
        ["BenchLengthMetres"] = "20",
        ["BenchWidthMetres"] = "12",
        ["LayoutRows"] = "3",
        ["LayoutColumns"] = "5",
        ["TimingOrder"] = "Rows",
        ["TimingIntervalMilliseconds"] = "25",
        ["DefaultAeciProductCode"] = "S300-VOLCANO",
        ["VibrationThreshold"] = "10",
        ["Holes[0].Id"] = holeId.ToString(),
        ["Holes[0].Number"] = "1",
        ["Holes[0].X"] = "0",
        ["Holes[0].Y"] = "0",
        ["Holes[0].Depth"] = "10",
        ["Holes[0].ExplosiveProductId"] = string.Empty,
        ["Holes[0].AeciProductCode"] = "S300-VOLCANO",
        ["Holes[0].ProductDensityGramsPerCc"] = "1.18",
        ["Holes[0].Charge"] = "5",
        ["Holes[0].Stemming"] = "2",
        ["Holes[0].Delay"] = "25",
        ["Calculation.DelayWindowMilliseconds"] = "50",
        ["Calculation.SubdrillMetres"] = "1.5",
        ["Calculation.ReceptorDistanceMetres"] = "100",
        ["Calculation.PpvSiteCoefficient"] = "100",
        ["Calculation.PpvDecayExponent"] = "1.6",
        ["Calculation.FlyrockLaunchSpeedMetresPerSecond"] = "20",
        ["Calculation.FlyrockLaunchAngleDegrees"] = "45",
        ["Calculation.FlyrockLaunchHeightMetres"] = "0",
        ["Calculation.ExclusionRadiusMetres"] = "50"
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
