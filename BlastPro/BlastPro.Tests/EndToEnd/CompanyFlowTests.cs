using System.Net;
using System.Net.Http.Json;
using BlastPro.Api.Models.Dtos;
using BlastPro.Tests.Support;
using Microsoft.AspNetCore.WebUtilities;

namespace BlastPro.Tests.EndToEnd;

public sealed class CompanyFlowTests
{
    [Fact]
    public async Task Main_user_registers_confirms_and_signs_in_from_shared_login_page()
    {
        await using var api = new ApiTestHost();
        await api.InitializeDatabaseAsync();
        await using var mvc = new MvcTestHost(api);
        using var browser = mvc.Browser();
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
        Assert.Contains("Confirm your email before signing in", await browser.GetStringAsync(created.Headers.Location));
        var confirmationPath = QueryHelpers.AddQueryString("/Account/ConfirmEmail", new Dictionary<string, string?>
            { ["email"] = request.Email, ["token"] = api.AccountMailbox.Confirmations[request.Email] });
        // Opening a link does not consume it; the confirmation form supplies CSRF protection.
        Assert.Contains("Confirm email", await browser.GetStringAsync(confirmationPath));
        var confirmed = await BrowserForms.SubmitAsync(browser, confirmationPath, "/Account/ConfirmEmail", new()
            { ["Email"] = request.Email, ["Token"] = api.AccountMailbox.Confirmations[request.Email] });
        Assert.Equal(HttpStatusCode.Redirect, confirmed.StatusCode);
        var login = await SignIn(browser, request.Email);
        Assert.Equal("/Dashboard/Index", login.Headers.Location?.OriginalString);
        var company = await browser.GetStringAsync("/Company/Index");
        Assert.Contains("0 of 5 active Blasters", company);
        Assert.Contains(request.CompanyName, company);
        Assert.Contains("Add Blaster", company);
    }

    [Fact]
    public async Task Main_user_invites_blaster_and_blaster_sets_password_through_form()
    {
        await using var api = new ApiTestHost();
        using var main = api.Client();
        var registration = await CompanyTestSetup.CreateMainAsync(api, main);
        await using var mvc = new MvcTestHost(api);
        using var mainBrowser = mvc.Browser();
        await SignIn(mainBrowser, registration.Email);
        var email = $"new-{Guid.NewGuid():N}@example.test";
        var createPage = await mainBrowser.GetStringAsync("/Blasters/Create");
        Assert.DoesNotContain("name=\"Password\"", createPage);
        var invited = await BrowserForms.SubmitAsync(mainBrowser, "/Blasters/Create", "/Blasters/Create", new()
        {
            ["FullName"] = "Invited Blaster", ["Email"] = email,
            ["PhoneNumber"] = "+27 82 555 1234", ["CertificationId"] = "TEST-CERT"
        });
        Assert.Equal(HttpStatusCode.Redirect, invited.StatusCode);
        Assert.Contains("1 of 5 active Blasters", await mainBrowser.GetStringAsync("/Company/Index"));
        using var blasterBrowser = mvc.Browser();
        var token = api.AccountMailbox.Invitations[email];
        var path = QueryHelpers.AddQueryString("/Account/AcceptInvitation", new Dictionary<string, string?> { ["email"] = email, ["token"] = token });
        var fields = new Dictionary<string, string>
            { ["Email"] = email, ["Token"] = token, ["Password"] = ApiTestHost.Password, ["ConfirmPassword"] = "DifferentPassword123!" };
        var mismatch = await BrowserForms.SubmitAsync(blasterBrowser, path, "/Account/AcceptInvitation", fields);
        Assert.Contains("Passwords do not match", await mismatch.Content.ReadAsStringAsync());
        fields["ConfirmPassword"] = ApiTestHost.Password;
        var accepted = await BrowserForms.SubmitAsync(blasterBrowser, path, "/Account/AcceptInvitation", fields);
        Assert.Equal(HttpStatusCode.Redirect, accepted.StatusCode);
        await SignIn(blasterBrowser, email);
        Assert.DoesNotContain("href=\"/Company/Index\"", await blasterBrowser.GetStringAsync("/Dashboard/Index"));
        foreach (var adminPath in new[] { "/Company/Index", "/Company/Edit", "/Blasters/Create" })
        {
            var denied = await blasterBrowser.GetAsync(adminPath);
            Assert.Equal(HttpStatusCode.Redirect, denied.StatusCode);
            Assert.Contains("AccessDenied", denied.Headers.Location!.OriginalString);
        }
        Assert.Contains("Invited Blaster", await blasterBrowser.GetStringAsync("/Profile/Index"));
    }

    [Fact]
    public async Task Company_blaster_and_profile_forms_save_permitted_details_and_status()
    {
        await using var api = new ApiTestHost();
        using var main = api.Client();
        var registration = await CompanyTestSetup.CreateMainAsync(api, main);
        var blaster = await CompanyTestSetup.InviteAsync(main);
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
    public async Task New_forms_require_antiforgery_tokens_and_invalid_links_offer_recovery()
    {
        await using var api = new ApiTestHost();
        await using var mvc = new MvcTestHost(api);
        using var browser = mvc.Browser();
        foreach (var path in new[] { "/Account/CreateCompany", "/Account/ConfirmEmail", "/Account/ResendConfirmation", "/Account/AcceptInvitation" })
            Assert.Equal(HttpStatusCode.BadRequest, (await browser.PostAsync(path, new FormUrlEncodedContent(new Dictionary<string, string>()))).StatusCode);
        Assert.Contains("Request a new link", await browser.GetStringAsync("/Account/ConfirmEmail"));
        Assert.Contains("Ask your company administrator", await browser.GetStringAsync("/Account/AcceptInvitation"));
    }

    private static Task<HttpResponseMessage> SignIn(HttpClient browser, string email)
        => BrowserForms.SubmitAsync(browser, "/Account/Login", "/Account/Login", new()
            { ["Email"] = email, ["Password"] = ApiTestHost.Password });
}
