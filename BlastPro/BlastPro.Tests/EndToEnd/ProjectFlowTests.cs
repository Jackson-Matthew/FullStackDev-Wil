
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
        Assert.Contains($"/Projects/Details/{projectId}", dashboardHtml);
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
        Assert.Contains("Hole Pattern Data", html);
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
                ["VibrationThreshold"] = "9",
                ["Holes[0].Id"] = holeId.ToString(),
                ["Holes[0].Number"] = "1",
                ["Holes[0].X"] = "4",
                ["Holes[0].Y"] = "5",
                ["Holes[0].Depth"] = "12",
                ["Holes[0].Charge"] = "7",
                ["Holes[0].Stemming"] = "3",
                ["Holes[0].Delay"] = "50"
            });

        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        var savedHtml = await saved.Content.ReadAsStringAsync();
        Assert.Contains("Draft layout saved", savedHtml);

        using var checkScope = api.Services.CreateScope();
        var checkDb = checkScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var updatedProject = await checkDb.BlastProjects.SingleAsync(p => p.Id == projectId);
        var updatedHole = await checkDb.BlastHoles.SingleAsync(h => h.Id == holeId);
        Assert.Equal(2.8m, updatedProject.RockDensity);
        Assert.Equal(12, updatedHole.Depth);
        Assert.Equal(7, updatedHole.ChargeKg);
        Assert.Equal(50, updatedHole.DelayMilliseconds);
    }
}
