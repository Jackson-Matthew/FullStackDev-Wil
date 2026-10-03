using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using BlastPro.Api.Models.Dtos;
using BlastPro.Tests.Support;

namespace BlastPro.Tests.EndToEnd;

public sealed class CompanyFlowTests
{
    [Fact]
    public async Task Create_company_shows_field_errors_beside_inputs()
    {
        await using var api = new ApiTestHost();
        await api.InitializeDatabaseAsync();
        await using var mvc = new MvcTestHost(api);
        using var browser = mvc.Browser();
        var request = CompanyTestSetup.Registration();
        var fields = new Dictionary<string, string>
        {
            ["CompanyName"] = request.CompanyName, ["RegistrationNumber"] = request.RegistrationNumber,
            ["ContactEmail"] = request.ContactEmail, ["ContactPhone"] = request.ContactPhone,
            ["FullName"] = request.FullName, ["Email"] = "invalid-email",
            ["Password"] = request.Password, ["ConfirmPassword"] = request.Password
        };

        var invalidEmail = await BrowserForms.SubmitAsync(browser, "/Account/CreateCompany", "/Account/CreateCompany", fields);
        var html = await invalidEmail.Content.ReadAsStringAsync();
        Assert.Contains("not a valid e-mail address", FieldError(html, "Email"));
        Assert.DoesNotContain("validation-summary-errors", html);

        fields["Email"] = request.Email;
        fields["Password"] = fields["ConfirmPassword"] = "weakpassword";
        var weakPassword = await BrowserForms.SubmitAsync(browser, "/Account/CreateCompany", "/Account/CreateCompany", fields);
        html = await weakPassword.Content.ReadAsStringAsync();
        Assert.Contains("non alphanumeric", FieldError(html, "Password"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("uppercase", FieldError(html, "Password"), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("validation-summary-errors", html);

        fields["Password"] = fields["ConfirmPassword"] = request.Password;
        var created = await BrowserForms.SubmitAsync(browser, "/Account/CreateCompany", "/Account/CreateCompany", fields);
        Assert.Equal(HttpStatusCode.Redirect, created.StatusCode);
        using var duplicateBrowser = mvc.Browser();
        var duplicate = await BrowserForms.SubmitAsync(duplicateBrowser, "/Account/CreateCompany", "/Account/CreateCompany", fields);
        html = await duplicate.Content.ReadAsStringAsync();
        Assert.Contains("already registered", FieldError(html, "RegistrationNumber"));
        Assert.DoesNotContain("validation-summary-errors", html);

        fields["RegistrationNumber"] = Guid.NewGuid().ToString("N");
        var duplicateEmail = await BrowserForms.SubmitAsync(duplicateBrowser, "/Account/CreateCompany", "/Account/CreateCompany", fields);
        html = await duplicateEmail.Content.ReadAsStringAsync();
        Assert.Contains("already registered", FieldError(html, "Email"));
        Assert.DoesNotContain("validation-summary-errors", html);
    }

    private static string FieldError(string html, string field)
    {
        var match = Regex.Match(html, $"<span[^>]*data-valmsg-for=\"{field}\"[^>]*>[^<]*</span>");
        Assert.True(match.Success, $"No validation span found for {field}.");
        return match.Value;
    }

    [Fact]
    public async Task Create_company_reports_when_api_is_unavailable()
    {
        await using var api = new ApiTestHost();
        await using var mvc = new MvcTestHost(api, offline: true);
        using var browser = mvc.Browser();
        var request = CompanyTestSetup.Registration();
        var fields = new Dictionary<string, string>
        {
            ["CompanyName"] = request.CompanyName, ["RegistrationNumber"] = request.RegistrationNumber,
            ["ContactEmail"] = request.ContactEmail, ["ContactPhone"] = request.ContactPhone,
            ["FullName"] = request.FullName, ["Email"] = request.Email,
            ["Password"] = request.Password, ["ConfirmPassword"] = request.Password
        };

        var response = await BrowserForms.SubmitAsync(browser, "/Account/CreateCompany", "/Account/CreateCompany", fields);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("The connection to the BlastPro server failed", html);
        Assert.Contains("Create Company", html);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Main_user_registers_and_is_signed_in_immediately(bool sqlServer)
    {
        await using var api = new ApiTestHost(sqlServer: sqlServer);
        await api.InitializeDatabaseAsync();
        await using var mvc = new MvcTestHost(api);
        using var browser = mvc.Browser();
        browser.Timeout = TimeSpan.FromSeconds(15);
        Assert.Contains("Create Company", await browser.GetStringAsync("/Account/Login"));
        var request = CompanyTestSetup.Registration();
        var fields = new Dictionary<string, string>
        {
            ["CompanyName"] = request.CompanyName, ["RegistrationNumber"] = request.RegistrationNumber,
            ["ContactEmail"] = request.ContactEmail, ["ContactPhone"] = request.ContactPhone, ["Address"] = request.Address!,
            ["FullName"] = request.FullName, ["Email"] = request.Email,
            ["Password"] = request.Password, ["ConfirmPassword"] = request.Password
        };
        var created = await BrowserForms.SubmitAsync(browser, "/Account/CreateCompany", "/Account/CreateCompany", fields);
        Assert.Equal(HttpStatusCode.Redirect, created.StatusCode);
        Assert.Equal("/Dashboard/Index", created.Headers.Location?.OriginalString);
        Assert.Contains("Search Projects", await browser.GetStringAsync(created.Headers.Location));
        var company = await browser.GetStringAsync("/Company/Index");
        Assert.Contains("0 of 5 active Blasters", company);
        Assert.Contains(request.CompanyName, company);
        Assert.Contains("Add New Blaster", company);
    }

    [Fact]
    public async Task Main_user_creates_blaster_with_password_and_blaster_signs_in()
    {
        await using var api = new ApiTestHost();
        using var main = api.Client();
        var registration = await CompanyTestSetup.CreateMainAsync(api, main);
        await using var mvc = new MvcTestHost(api);
        using var mainBrowser = mvc.Browser();
        await SignIn(mainBrowser, registration.Email);
        var email = $"new-{Guid.NewGuid():N}@example.test";
        var createPage = await mainBrowser.GetStringAsync("/Blasters/Create");
        Assert.Contains("name=\"Password\"", createPage);
        var created = await BrowserForms.SubmitAsync(mainBrowser, "/Blasters/Create", "/Blasters/Create", new()
        {
            ["Email"] = email, ["Password"] = ApiTestHost.Password,
            ["ConfirmPassword"] = ApiTestHost.Password
        });
        Assert.Equal(HttpStatusCode.Redirect, created.StatusCode);
        Assert.Contains("1 of 5 active Blasters", await mainBrowser.GetStringAsync("/Company/Index"));
        Assert.Contains("Set password", await mainBrowser.GetStringAsync("/Company/Index"));
        using var blasterBrowser = mvc.Browser();
        await SignIn(blasterBrowser, email);
        Assert.DoesNotContain("href=\"/Company/Index\"", await blasterBrowser.GetStringAsync("/Dashboard/Index"));
        foreach (var adminPath in new[] { "/Company/Index", "/Company/Edit", "/Blasters/Create" })
        {
            var denied = await blasterBrowser.GetAsync(adminPath);
            Assert.Equal(HttpStatusCode.Redirect, denied.StatusCode);
            Assert.Contains("AccessDenied", denied.Headers.Location!.OriginalString);
        }
        Assert.Contains(email, await blasterBrowser.GetStringAsync("/Profile/Index"));

        var blasterId = Assert.Single((await main.GetFromJsonAsync<CompanyDto>("/api/company"))!.Blasters).Id;
        var passwordPath = $"/Blasters/SetPassword/{blasterId}";
        var passwordChange = await BrowserForms.SubmitAsync(mainBrowser, passwordPath, passwordPath, new()
        {
            ["Email"] = email, ["Password"] = "UpdatedPassword123!",
            ["ConfirmPassword"] = "UpdatedPassword123!"
        });
        Assert.Equal(HttpStatusCode.Redirect, passwordChange.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await main.PostAsJsonAsync("/api/auth/login",
            new { email, password = ApiTestHost.Password })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await main.PostAsJsonAsync("/api/auth/login",
            new { email, password = "UpdatedPassword123!" })).StatusCode);
    }

    [Fact]
    public async Task Company_blaster_and_profile_forms_save_permitted_details_and_status()
    {
        await using var api = new ApiTestHost();
        using var main = api.Client();
        var registration = await CompanyTestSetup.CreateMainAsync(api, main);
        var blaster = await CompanyTestSetup.CreateBlasterAsync(main);
        await using var mvc = new MvcTestHost(api);
        using var browser = mvc.Browser();
        await SignIn(browser, registration.Email);
        Assert.Equal(HttpStatusCode.Redirect, (await BrowserForms.SubmitAsync(browser, "/Company/Edit", "/Company/Edit", new()
        {
            ["ContactEmail"] = "updated@example.test", ["ContactPhone"] = "+27 11 555 4321", ["Address"] = "Pretoria"
        })).StatusCode);
        var edit = $"/Blasters/Edit/{blaster.Id}";
        Assert.Equal(HttpStatusCode.Redirect, (await BrowserForms.SubmitAsync(browser, edit, edit, new()
        {
            ["FullName"] = "Edited Blaster", ["PhoneNumber"] = "+27 82 555 9999", ["CertificationId"] = "EDITED-CERT"
        })).StatusCode);
        var company = await browser.GetStringAsync("/Company/Index");
        Assert.Contains("updated@example.test", company);
        Assert.Contains("Edited Blaster", company);
        Assert.Contains("EDITED-CERT", company);
        Assert.Equal(HttpStatusCode.Redirect, (await BrowserForms.SubmitAsync(browser, "/Company/Index", $"/Blasters/SetStatus/{blaster.Id}", new() { ["IsActive"] = "false" })).StatusCode);
        Assert.Contains("0 of 5 active Blasters", await browser.GetStringAsync("/Company/Index"));
        Assert.Equal(HttpStatusCode.Redirect, (await BrowserForms.SubmitAsync(browser, "/Profile/Edit", "/Profile/Edit", new()
        {
            ["FullName"] = "Updated Main User", ["NickName"] = "Sam", ["Country"] = "South Africa", ["TimeZoneId"] = "Africa/Johannesburg"
        })).StatusCode);
        var profile = await browser.GetStringAsync("/Profile/Index");
        Assert.Contains("Updated Main User", profile);
        Assert.Contains("South Africa", profile);
        Assert.Contains("Africa/Johannesburg", profile);
        Assert.Contains(registration.CompanyName, profile);
    }

    [Fact]
    public async Task New_forms_require_antiforgery_tokens()
    {
        await using var api = new ApiTestHost();
        await using var mvc = new MvcTestHost(api);
        using var browser = mvc.Browser();
        Assert.Equal(HttpStatusCode.BadRequest, (await browser.PostAsync("/Account/CreateCompany",
            new FormUrlEncodedContent(new Dictionary<string, string>()))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await browser.GetAsync("/Account/AcceptInvitation")).StatusCode);
    }

    private static Task<HttpResponseMessage> SignIn(HttpClient browser, string email)
        => BrowserForms.SubmitAsync(browser, "/Account/Login", "/Account/Login", new()
            { ["Email"] = email, ["Password"] = ApiTestHost.Password });
}
