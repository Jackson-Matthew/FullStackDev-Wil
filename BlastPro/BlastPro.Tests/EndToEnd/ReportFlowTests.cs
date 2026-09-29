
using System.Net;
using BlastPro.Api.Data;
using BlastPro.Api.Models.Entities;
using BlastPro.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace BlastPro.Tests.EndToEnd;

public sealed class ReportFlowTests
{
    [Fact]
    public async Task Preview_shows_project_empty_state_without_sample_report_data()
    {
        await using var api = new ApiTestHost();
        var user = await api.AddUserAsync();
        int projectId;
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var project = new BlastProject
            {
                CompanyId = user.CompanyId, OwnerId = user.Id, Name = "Report draft",
                SiteLocation = "Test site", BlastType = "Surface"
            };
            db.BlastProjects.Add(project);
            await db.SaveChangesAsync();
            projectId = project.Id;
        }
        await using var mvc = new MvcTestHost(api);
        using var browser = mvc.Browser();

        var login = await BrowserForms.SubmitAsync(browser,
            "/Account/Login", "/Account/Login", new Dictionary<string, string>
            {
                ["Email"] = user.Email!,
                ["Password"] = ApiTestHost.Password,
                ["RememberMe"] = "False"
            });
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);

        var list = await browser.GetAsync("/Reports/Index");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Contains("Report draft", await list.Content.ReadAsStringAsync());

        var response = await browser.GetAsync($"/Reports/Preview?projectId={projectId}");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("no saved calculation yet", html);
        Assert.DoesNotContain("TestCompany", html);
        Assert.DoesNotContain("ANFO Pack", html);
        Assert.DoesNotContain("Emulsion Max", html);
    }
}
