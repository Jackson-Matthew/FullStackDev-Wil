
using System.Net;
using BlastPro.Tests.Support;

namespace BlastPro.Tests.EndToEnd;

public sealed class ReportFlowTests
{
    [Fact]
    public async Task Preview_shows_empty_state_without_sample_report_data()
    {
        await using var api = new ApiTestHost();
        var user = await api.AddUserAsync();
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

        var response = await browser.GetAsync("/Reports/Preview?projectId=123");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("No results to report on", html);
        Assert.DoesNotContain("TestCompany", html);
        Assert.DoesNotContain("ANFO Pack", html);
        Assert.DoesNotContain("Emulsion Max", html);
    }
}
