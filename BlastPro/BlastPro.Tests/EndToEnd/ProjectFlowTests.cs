
using System.Net;
using BlastPro.Api.Data;
using BlastPro.Api.Models.Entities;
using BlastPro.Api.Models.Enums;
using BlastPro.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BlastPro.Tests.EndToEnd;

public sealed class ProjectFlowTests
{
    [Fact]
    public async Task Creating_a_project_opens_its_pattern_design()
    {
        await using var api = new ApiTestHost();
        var user = await api.AddUserAsync();
        await using var mvc = new MvcTestHost(api);
        using var browser = mvc.Browser();
        await BrowserForms.SubmitAsync(browser, "/Account/Login", "/Account/Login",
            new Dictionary<string, string> { ["Email"] = user.Email!, ["Password"] = ApiTestHost.Password,
                ["RememberMe"] = "False" });

        var created = await BrowserForms.SubmitAsync(browser, "/Projects/Create", "/Projects/Create",
            new Dictionary<string, string>
            {
                ["Name"] = "New bench",
                ["SiteLocation"] = "Test site",
                ["BlastType"] = "Production"
            });
        Assert.Equal(HttpStatusCode.Redirect, created.StatusCode);
        Assert.Contains("PatternDesign", created.Headers.Location!.ToString());
        var design = await browser.GetAsync(created.Headers.Location!);
        Assert.Contains("New bench — Pattern Design", await design.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Draft_can_be_saved_while_bench_and_site_inputs_are_incomplete()
    {
        await using var api = new ApiTestHost();
        var user = await api.AddUserAsync();
        int projectId;
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var project = new BlastProject
            {
                CompanyId = user.CompanyId, OwnerId = user.Id, Name = "Partial bench",
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
        var response = await BrowserForms.SubmitAsync(browser,
            $"/PatternDesign/Index?projectId={projectId}", "/PatternDesign/SaveDraft",
            new Dictionary<string, string>
            {
                ["ProjectId"] = projectId.ToString(), ["ProjectName"] = "Partial bench",
                ["RockType"] = "Granite", ["RockDensity"] = "", ["Burden"] = "", ["Spacing"] = "",
                ["BenchLengthMetres"] = "24", ["BenchWidthMetres"] = "",
                ["TimingOrder"] = "Sequential", ["PatternType"] = "Rectangular",
                ["VibrationThreshold"] = "", ["VibrationThresholdMode"] = "Manual",
                ["ReceptorStructureType"] = "Unspecified", ["continueToResults"] = "true"
            });
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("Results", response.Headers.Location!.ToString());
        var reopened = await browser.GetAsync(response.Headers.Location!);
        Assert.Contains("Pattern draft saved", await reopened.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Dashboard_and_project_view_show_the_real_status_details_and_holes()
    {
        await using var api = new ApiTestHost();
        var user = await api.AddUserAsync();
        int projectId;

        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var project = new BlastProject
            {
                CompanyId = user.CompanyId,
                OwnerId = user.Id,
                Name = "Calculated Project",
                SiteLocation = "North Bench",
                BlastType = "Production",
                RockType = "Granite",
                Status = ProjectStatus.Calculated
            };
            db.BlastProjects.Add(project);
            await db.SaveChangesAsync();
            db.BlastHoles.Add(new BlastHole
            {
                BlastProjectId = project.Id,
                HoleNumber = 1,
                XCoordinate = 4,
                YCoordinate = 5,
                Depth = 12,
                ChargeKg = 7,
                StemmingMetres = 3,
                DelayMilliseconds = 50
            });
            await db.SaveChangesAsync();
            projectId = project.Id;
        }

        await using var mvc = new MvcTestHost(api);
        using var browser = mvc.Browser();
        await BrowserForms.SubmitAsync(
            browser,
            "/Account/Login",
            "/Account/Login",
            new Dictionary<string, string>
            {
                ["Email"] = user.Email!,
                ["Password"] = ApiTestHost.Password,
                ["RememberMe"] = "False"
            });

        var dashboard = await browser.GetAsync("/Dashboard/Index");
        var dashboardHtml = await dashboard.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, dashboard.StatusCode);
        Assert.Contains("Calculated Project", dashboardHtml);
        Assert.Contains("Calculated", dashboardHtml);
        Assert.Contains($"/Reports/Preview?projectId={projectId}", dashboardHtml);
        Assert.Contains($"/PatternDesign/Index?projectId={projectId}", dashboardHtml);

        var details = await browser.GetAsync($"/Projects/Details/{projectId}");
        var detailsHtml = await details.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, details.StatusCode);
        Assert.Contains("North Bench", detailsHtml);
        Assert.Contains("Production", detailsHtml);
        Assert.Contains("Granite", detailsHtml);
        Assert.Contains("Hole Pattern", detailsHtml);
        Assert.Contains(">12<", detailsHtml);
        Assert.Contains(">50<", detailsHtml);
    }

    [Fact]
    public async Task Pattern_design_navbar_route_renders_the_existing_project_and_hole_ui()
    {
        await using var api = new ApiTestHost();
        var user = await api.AddUserAsync();

        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var project = new BlastProject
            {
                CompanyId = user.CompanyId,
                OwnerId = user.Id,
                Name = "Pattern Test",
                SiteLocation = "Test Site",
                BlastType = "Surface",
                RockType = "Granite",
                RockDensity = 2.7m,
                Burden = 3m,
                Spacing = 3.5m,
                VibrationThreshold = 10m,
                Status = ProjectStatus.Draft
            };
            db.BlastProjects.Add(project);
            await db.SaveChangesAsync();
            db.BlastHoles.Add(new BlastHole
            {
                BlastProjectId = project.Id,
                HoleNumber = 1,
                Depth = 10,
                ChargeKg = 5,
                StemmingMetres = 2,
                DelayMilliseconds = 25
            });
            await db.SaveChangesAsync();
        }

        await using var mvc = new MvcTestHost(api);
        using var browser = mvc.Browser();
        await BrowserForms.SubmitAsync(
            browser,
            "/Account/Login",
            "/Account/Login",
            new Dictionary<string, string>
            {
                ["Email"] = user.Email!,
                ["Password"] = ApiTestHost.Password,
                ["RememberMe"] = "False"
            });

        var open = await browser.GetAsync("/PatternDesign/Index");
        Assert.Equal(HttpStatusCode.Redirect, open.StatusCode);
        Assert.Contains("projectId=", open.Headers.Location?.OriginalString);

        var page = await browser.GetAsync(open.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        var html = await page.Content.ReadAsStringAsync();
        Assert.Contains("Pattern Test", html);
        Assert.Contains("Editable hole table", html);
        Assert.Contains("Bench and hole layout", html);
        Assert.Contains("LayoutRows", html);
        Assert.Contains("LayoutColumns", html);
        Assert.Contains("benchPlan", html);
        Assert.Contains("Use USBM suggestion", html);
        Assert.Contains("Charge (kg)", html);
        Assert.Contains("Holes[0].Depth", html);
    }

    [Fact]
    public async Task Save_draft_updates_the_pattern_and_holes_through_mvc_and_api()
    {
        await using var api = new ApiTestHost();
        var user = await api.AddUserAsync();
        int projectId;
        int holeId;

        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var project = new BlastProject
            {
                CompanyId = user.CompanyId,
                OwnerId = user.Id,
                Name = "Save Test",
                SiteLocation = "Test Site",
                BlastType = "Surface",
                Status = ProjectStatus.Draft
            };
            db.BlastProjects.Add(project);
            await db.SaveChangesAsync();
            var hole = new BlastHole
            {
                BlastProjectId = project.Id,
                HoleNumber = 1,
                Depth = 10,
                ChargeKg = 5,
                StemmingMetres = 2,
                DelayMilliseconds = 25
            };
            db.BlastHoles.Add(hole);
            await db.SaveChangesAsync();
            projectId = project.Id;
            holeId = hole.Id;
        }

        await using var mvc = new MvcTestHost(api);
        using var browser = mvc.Browser();
        await BrowserForms.SubmitAsync(
            browser,
            "/Account/Login",
            "/Account/Login",
            new Dictionary<string, string>
            {
                ["Email"] = user.Email!,
                ["Password"] = ApiTestHost.Password,
                ["RememberMe"] = "False"
            });

        var saved = await BrowserForms.SubmitAsync(
            browser,
            $"/PatternDesign/Index?projectId={projectId}",
            "/PatternDesign/SaveDraft",
            new Dictionary<string, string>
            {
                ["ProjectId"] = projectId.ToString(),
                ["ProjectName"] = "Save Test",
                ["Status"] = "Draft",
                ["RowVersion"] = string.Empty,
                ["RockType"] = "Granite",
                ["RockDensity"] = "2.8",
                ["Burden"] = "3.2",
                ["Spacing"] = "3.6",
                ["BenchLengthMetres"] = "20",
                ["BenchWidthMetres"] = "12",
                ["LayoutRows"] = "3",
                ["LayoutColumns"] = "5",
                ["TimingOrder"] = "RowGroups",
                ["TimingIntervalMilliseconds"] = "25",
                ["PatternType"] = "Staggered",
                ["ReferenceExplosiveFamily"] = "S100",
                ["DefaultAeciProductCode"] = "PG-ECO-30",
                ["LoadingDensityGramsPerCc"] = "1.15",
                ["Calculation.DelayWindowMilliseconds"] = "25",
                ["Calculation.SubdrillMetres"] = "0.8",
                ["Calculation.ReceptorDistanceMetres"] = "400",
                ["Calculation.PpvSiteCoefficient"] = "500",
                ["Calculation.PpvDecayExponent"] = "1.5",
                ["VibrationThreshold"] = "9",
                ["Holes[0].Id"] = holeId.ToString(),
                ["Holes[0].Number"] = "1",
                ["Holes[0].X"] = "4",
                ["Holes[0].Y"] = "5",
                ["Holes[0].Depth"] = "12",
                ["Holes[0].DiameterMillimetres"] = "115",
                ["Holes[0].AeciProductCode"] = "PG-ECO-30",
                ["Holes[0].ProductDensityGramsPerCc"] = "1.19",
                ["Holes[0].Charge"] = "7",
                ["Holes[0].Stemming"] = "3",
                ["Holes[0].Delay"] = "50"
            });

        Assert.Equal(HttpStatusCode.Redirect, saved.StatusCode);
        var reopened = await browser.GetAsync(saved.Headers.Location!);
        Assert.Contains("Pattern draft saved", await reopened.Content.ReadAsStringAsync());

        using var checkScope = api.Services.CreateScope();
        var checkDb = checkScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var updatedProject = await checkDb.BlastProjects.SingleAsync(p => p.Id == projectId);
        var updatedHole = await checkDb.BlastHoles.SingleAsync(h => h.Id == holeId);
        Assert.Equal(2.8m, updatedProject.RockDensity);
        Assert.Equal(20m, updatedProject.BenchLengthMetres);
        Assert.Equal(12m, updatedProject.BenchWidthMetres);
        Assert.Equal(3, updatedProject.LayoutRows);
        Assert.Equal(5, updatedProject.LayoutColumns);
        Assert.Equal("RowGroups", updatedProject.TimingOrder);
        Assert.Equal(25, updatedProject.TimingIntervalMilliseconds);
        Assert.Equal("Staggered", updatedProject.PatternType);
        Assert.Equal("S100", updatedProject.ReferenceExplosiveFamily);
        Assert.Equal("PG-ECO-30", updatedProject.DefaultAeciProductCode);
        Assert.Equal(1.15m, updatedProject.LoadingDensityGramsPerCc);
        Assert.Equal(25, updatedProject.DelayWindowMilliseconds);
        Assert.Equal(0.8m, updatedProject.SubdrillMetres);
        Assert.Equal(400m, updatedProject.ReceptorDistanceMetres);
        Assert.Equal(500m, updatedProject.PpvSiteCoefficient);
        Assert.Equal(1.5m, updatedProject.PpvDecayExponent);
        Assert.Equal(12, updatedHole.Depth);
        Assert.Equal(115m, updatedHole.DiameterMillimetres);
        Assert.Equal("PG-ECO-30", updatedHole.AeciProductCode);
        Assert.Equal(1.19m, updatedHole.ProductDensityGramsPerCc);
        Assert.Equal(7, updatedHole.ChargeKg);
        Assert.Equal(50, updatedHole.DelayMilliseconds);
    }
}
