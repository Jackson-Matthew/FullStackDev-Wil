using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using BlastPro.Tests.Support;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BlastPro.Tests.Integration;

public class AccountPageTests
{
    private static Dictionary<string, string> LoginFields(string email, bool remember = false) => new()
    {
        ["Email"] = email, ["Password"] = ApiTestHost.Password, ["RememberMe"] = remember.ToString()
    };

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Login_forwards_token_to_dashboard_and_remember_me_controls_persistence(bool remember)
    {
        await using var api = new ApiTestHost();
        var user = await api.AddUserAsync();
        await using var mvc = new MvcTestHost(api);
        using var browser = mvc.Browser();
        var result = await BrowserForms.SubmitAsync(browser, "/Account/Login", "/Account/Login", LoginFields(user.Email!, remember));
        Assert.Equal(HttpStatusCode.Redirect, result.StatusCode);
        Assert.Equal("/Dashboard/Index", result.Headers.Location?.OriginalString);
        var cookie = Assert.Single(result.Headers.GetValues("Set-Cookie"), x => x.StartsWith(".AspNetCore.Cookies="));
        Assert.Equal(remember, cookie.Contains("expires=", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("secure", cookie);
        Assert.Contains("httponly", cookie);
        var options = mvc.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);
        var value = cookie.Split(';')[0].Split('=', 2)[1];
        var ticket = options.TicketDataFormat.Unprotect(Uri.UnescapeDataString(value));
        Assert.NotNull(ticket);
        Assert.False(ticket.Properties.AllowRefresh);
        Assert.True(ticket.Properties.ExpiresUtc <= DateTimeOffset.UtcNow.AddMinutes(10));
        var dashboard = await browser.GetAsync("/Dashboard/Index");
        Assert.Equal(HttpStatusCode.OK, dashboard.StatusCode);
        var html = await dashboard.Content.ReadAsStringAsync();
        Assert.Contains("My Projects", html);
        Assert.DoesNotContain("Could not load projects", html);
    }

    [Fact]
    public async Task Protected_routes_and_logout_return_to_login()
    {
        await using var api = new ApiTestHost();
        var user = await api.AddUserAsync();
        await using var mvc = new MvcTestHost(api);
        using var browser = mvc.Browser();
        var protectedPage = await browser.GetAsync("/Dashboard/Index");
        Assert.Equal(HttpStatusCode.Redirect, protectedPage.StatusCode);
        Assert.Contains("/Account/Login", protectedPage.Headers.Location!.OriginalString);
        await BrowserForms.SubmitAsync(browser, "/Account/Login", "/Account/Login", LoginFields(user.Email!));
        var noCsrf = await browser.PostAsync("/Account/Logout", new FormUrlEncodedContent(new Dictionary<string, string>()));
        Assert.Equal(HttpStatusCode.BadRequest, noCsrf.StatusCode);
        var logout = await BrowserForms.SubmitAsync(browser, "/Dashboard/Index", "/Account/Logout", new());
        Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await browser.GetAsync("/Dashboard/Index")).StatusCode);
    }

    [Fact]
    public async Task Rejected_api_token_clears_cookie_and_prompts_login()
    {
        await using var api = new ApiTestHost();
        var user = await api.AddUserAsync();
        await using var mvc = new MvcTestHost(api);
        using var browser = mvc.Browser();
        await BrowserForms.SubmitAsync(browser, "/Account/Login", "/Account/Login", LoginFields(user.Email!));
        await api.ChangeUserAsync(user.Id, async (manager, current) => await manager.UpdateSecurityStampAsync(current));
        var dashboard = await browser.GetAsync("/Dashboard/Index");
        Assert.Equal(HttpStatusCode.Redirect, dashboard.StatusCode);
        Assert.Contains("expired=True", dashboard.Headers.Location!.OriginalString);
        var login = await browser.GetStringAsync(dashboard.Headers.Location);
        Assert.Contains("Your session has ended", login);
        Assert.Equal(HttpStatusCode.Redirect, (await browser.GetAsync("/Dashboard/Index")).StatusCode);
    }

    [Fact]
    public async Task Expired_cookie_is_rejected_before_protected_page()
    {
        await using var api = new ApiTestHost();
        var user = await api.AddUserAsync();
        await using var mvc = new MvcTestHost(api);
        using var browser = mvc.Browser();
        var login = await BrowserForms.SubmitAsync(browser, "/Account/Login", "/Account/Login", LoginFields(user.Email!));
        var cookie = Assert.Single(login.Headers.GetValues("Set-Cookie"), x => x.StartsWith(".AspNetCore.Cookies="));
        var options = mvc.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);
        var ticket = options.TicketDataFormat.Unprotect(cookie.Split(';')[0].Split('=', 2)[1])!;
        ticket.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(-1);
        using var expiredBrowser = mvc.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false, HandleCookies = false });
        expiredBrowser.DefaultRequestHeaders.Add("Cookie", ".AspNetCore.Cookies=" + options.TicketDataFormat.Protect(ticket));
        var response = await expiredBrowser.GetAsync("/Dashboard/Index");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Api_outage_has_useful_messages_without_false_reset_confirmation()
    {
        await using var api = new ApiTestHost();
        await using var mvc = new MvcTestHost(api, offline: true);
        using var browser = mvc.Browser();
        var login = await BrowserForms.SubmitAsync(browser, "/Account/Login", "/Account/Login", LoginFields("user@example.test"));
        var html = await login.Content.ReadAsStringAsync();
        Assert.Contains("Sign in is temporarily unavailable", html);
        Assert.DoesNotContain("Invalid login attempt", html);
        Assert.DoesNotContain("Private network", html);
        var forgot = await BrowserForms.SubmitAsync(browser, "/Account/ForgotPassword", "/Account/ForgotPassword",
            new() { ["Email"] = "user@example.test" });
        Assert.Equal(HttpStatusCode.OK, forgot.StatusCode);
        Assert.Contains("Password reset is temporarily unavailable", await forgot.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Bad_credentials_are_generic_and_external_return_urls_are_ignored()
    {
        await using var api = new ApiTestHost();
        var user = await api.AddUserAsync();
        await using var mvc = new MvcTestHost(api);
        using var browser = mvc.Browser();
        var fields = LoginFields(user.Email!);
        fields["Password"] = "WrongPassword!";
        var failed = await BrowserForms.SubmitAsync(browser, "/Account/Login", "/Account/Login", fields);
        Assert.Contains("Invalid login attempt", await failed.Content.ReadAsStringAsync());
        fields = LoginFields(user.Email!);
        fields["ReturnUrl"] = "https://untrusted.example/";
        var success = await BrowserForms.SubmitAsync(browser, "/Account/Login", "/Account/Login", fields);
        Assert.Equal("/Dashboard/Index", success.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Password_reset_pages_complete_local_flow_and_validate_confirmation()
    {
        await using var api = new ApiTestHost();
        var user = await api.AddUserAsync(email: "blaster+reset@example.test");
        await using var mvc = new MvcTestHost(api);
        using var browser = mvc.Browser();
        var forgot = await BrowserForms.SubmitAsync(browser, "/Account/ForgotPassword", "/Account/ForgotPassword",
            new() { ["Email"] = user.Email! });
        Assert.Equal(HttpStatusCode.Redirect, forgot.StatusCode);
        var confirmation = await browser.GetStringAsync(forgot.Headers.Location);
        Assert.Contains("no email is sent", confirmation);
        var token = api.Mailbox.Messages[user.Email!];
        var path = QueryHelpers.AddQueryString("/Account/ResetPassword",
            new Dictionary<string, string?> { ["email"] = user.Email, ["token"] = token });
        var fields = new Dictionary<string, string>
        {
            ["Email"] = user.Email!, ["Token"] = token,
            ["Password"] = "ChangedPassword123!", ["ConfirmPassword"] = "DifferentPassword123!"
        };
        var mismatch = await BrowserForms.SubmitAsync(browser, path, "/Account/ResetPassword", fields);
        Assert.Contains("Passwords do not match", await mismatch.Content.ReadAsStringAsync());
        fields["ConfirmPassword"] = fields["Password"];
        var reset = await BrowserForms.SubmitAsync(browser, path, "/Account/ResetPassword", fields);
        Assert.Equal(HttpStatusCode.Redirect, reset.StatusCode);
        Assert.Contains("Your password has been reset", await browser.GetStringAsync(reset.Headers.Location));
        var loginFields = LoginFields(user.Email!);
        loginFields["Password"] = fields["Password"];
        Assert.Equal(HttpStatusCode.Redirect,
            (await BrowserForms.SubmitAsync(browser, "/Account/Login", "/Account/Login", loginFields)).StatusCode);
    }

    [Fact]
    public async Task Invalid_reset_links_and_password_policy_errors_have_recovery_links()
    {
        await using var api = new ApiTestHost();
        var user = await api.AddUserAsync();
        await using var mvc = new MvcTestHost(api);
        using var browser = mvc.Browser();
        Assert.Contains("Reset link unavailable", await browser.GetStringAsync("/Account/ResetPassword"));
        using var client = api.Client();
        var token = await api.RequestResetAsync(client, user.Email!);
        var path = QueryHelpers.AddQueryString("/Account/ResetPassword", new Dictionary<string, string?>
            { ["email"] = user.Email, ["token"] = token });
        var fields = new Dictionary<string, string>
            { ["Email"] = user.Email!, ["Token"] = token, ["Password"] = "weakpassword", ["ConfirmPassword"] = "weakpassword" };
        var weak = await BrowserForms.SubmitAsync(browser, path, "/Account/ResetPassword", fields);
        var html = await weak.Content.ReadAsStringAsync();
        Assert.Contains("Passwords must", html);
        Assert.DoesNotContain("{&quot;errors&quot;", html);
        fields["Token"] = "!invalid!";
        fields["Password"] = fields["ConfirmPassword"] = "ValidPassword123!";
        var invalid = await BrowserForms.SubmitAsync(browser, path, "/Account/ResetPassword", fields);
        Assert.Contains("Request a new reset link", await invalid.Content.ReadAsStringAsync());
        Assert.Contains("invalid or has expired", await invalid.Content.ReadAsStringAsync());
    }
}
