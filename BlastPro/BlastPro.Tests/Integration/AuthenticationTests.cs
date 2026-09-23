using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BlastPro.Tests.Support;

namespace BlastPro.Tests.Integration;

public class AuthenticationTests
{
    [Fact]
    public async Task Login_issues_token_that_can_access_protected_endpoint()
    {
        await using var host = new ApiTestHost();
        var user = await host.AddUserAsync();
        using var client = host.Client();
        var result = await host.LoginAsync(client, user.Email!);
        Assert.Equal(user.CompanyId, result.CompanyId);
        Assert.Contains("Blaster", result.Roles);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.Token);
        var me = await client.GetAsync("/api/auth/me");
        Assert.True(me.StatusCode == HttpStatusCode.OK, me.Headers.WwwAuthenticate.ToString());
    }

    [Fact]
    public async Task Unknown_wrong_password_inactive_and_locked_accounts_have_same_response()
    {
        await using var host = new ApiTestHost();
        var user = await host.AddUserAsync();
        var inactive = await host.AddUserAsync(false);
        using var client = host.Client();
        var unknown = await client.PostAsJsonAsync("/api/auth/login", new { email = "missing@example.test", password = ApiTestHost.Password });
        var expected = await unknown.Content.ReadAsStringAsync();
        foreach (var email in new[] { user.Email!, inactive.Email! })
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Wrong123!" });
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal(expected, await response.Content.ReadAsStringAsync());
        }
        var inactiveLogin = await client.PostAsJsonAsync("/api/auth/login", new { email = inactive.Email, password = ApiTestHost.Password });
        Assert.Equal(expected, await inactiveLogin.Content.ReadAsStringAsync());
        // One earlier failure plus four more triggers the configured five-attempt lockout.
        for (var i = 0; i < 4; i++)
            await client.PostAsJsonAsync("/api/auth/login", new { email = user.Email, password = "Wrong123!" });
        var locked = await client.PostAsJsonAsync("/api/auth/login", new { email = user.Email, password = ApiTestHost.Password });
        Assert.Equal(HttpStatusCode.Unauthorized, locked.StatusCode);
        Assert.Equal(expected, await locked.Content.ReadAsStringAsync());
        await host.ChangeUserAsync(user.Id, async (manager, current) =>
        {
            Assert.True(await manager.IsLockedOutAsync(current));
            await manager.SetLockoutEndDateAsync(current, DateTimeOffset.UtcNow.AddMinutes(-1));
        });
        await host.LoginAsync(client, user.Email!);
    }

    [Fact]
    public async Task Successful_login_clears_failed_attempt_count()
    {
        await using var host = new ApiTestHost();
        var user = await host.AddUserAsync();
        using var client = host.Client();
        await client.PostAsJsonAsync("/api/auth/login", new { email = user.Email, password = "Wrong123!" });
        await host.LoginAsync(client, user.Email!);
        await host.ChangeUserAsync(user.Id, async (manager, current) =>
            Assert.Equal(0, await manager.GetAccessFailedCountAsync(current)));
    }

    [Fact]
    public async Task Forgot_password_does_not_reveal_account_state_or_token()
    {
        await using var host = new ApiTestHost();
        var user = await host.AddUserAsync();
        var inactive = await host.AddUserAsync(false);
        using var client = host.Client();
        string? expected = null;
        foreach (var email in new[] { user.Email!, inactive.Email!, "missing@example.test" })
        {
            var result = await client.PostAsJsonAsync("/api/auth/forgot-password", new { email });
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            var body = await result.Content.ReadAsStringAsync();
            expected ??= body;
            Assert.Equal(expected, body);
            Assert.DoesNotContain(email, body);
            Assert.DoesNotContain("token", body.ToLowerInvariant());
        }
        Assert.Single(host.Mailbox.Messages);
        host.Mailbox.Available = false;
        foreach (var email in new[] { user.Email!, "missing@example.test" })
            Assert.Equal(HttpStatusCode.ServiceUnavailable,
                (await client.PostAsJsonAsync("/api/auth/forgot-password", new { email })).StatusCode);
    }

    [Fact]
    public async Task Reset_changes_password_rejects_reuse_and_revokes_previous_login()
    {
        await using var host = new ApiTestHost();
        var user = await host.AddUserAsync();
        using var client = host.Client();
        var login = await host.LoginAsync(client, user.Email!);
        var token = await host.RequestResetAsync(client, user.Email!);
        var data = new { email = user.Email, token, newPassword = "ChangedPassword123!" };
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/auth/reset-password", data)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/reset-password", data)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login",
            new { email = user.Email, password = ApiTestHost.Password })).StatusCode);
        await host.LoginAsync(client, user.Email!, data.newPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Expired_reset_token_is_rejected()
    {
        await using var host = new ApiTestHost(resetLifetime: TimeSpan.FromSeconds(-1));
        var user = await host.AddUserAsync();
        using var client = host.Client();
        var token = await host.RequestResetAsync(client, user.Email!);
        var response = await client.PostAsJsonAsync("/api/auth/reset-password",
            new { email = user.Email, token, newPassword = "ChangedPassword123!" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("expired", await response.Content.ReadAsStringAsync());
        await host.LoginAsync(client, user.Email!);
    }

    [Fact]
    public async Task Invalid_token_and_weak_password_have_readable_errors()
    {
        await using var host = new ApiTestHost();
        var user = await host.AddUserAsync();
        using var client = host.Client();
        var token = await host.RequestResetAsync(client, user.Email!);
        var weak = await client.PostAsJsonAsync("/api/auth/reset-password", new { email = user.Email, token, newPassword = "weakpassword" });
        Assert.Equal(HttpStatusCode.BadRequest, weak.StatusCode);
        Assert.Contains("Passwords must", await weak.Content.ReadAsStringAsync());
        var invalid = await client.PostAsJsonAsync("/api/auth/reset-password", new { email = user.Email, token = "!invalid!", newPassword = "ValidPassword123!" });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Contains("request a new link", await invalid.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Expired_token_and_deactivated_user_cannot_use_api()
    {
        await using var host = new ApiTestHost();
        var user = await host.AddUserAsync();
        using var client = host.Client();
        var login = await host.LoginAsync(client, user.Email!);
        await host.ChangeUserAsync(user.Id, async (manager, current) =>
        {
            current.IsActive = false;
            await manager.UpdateAsync(current);
        });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);

        await using var expiredHost = new ApiTestHost(tokenMinutes: -5);
        var expiredUser = await expiredHost.AddUserAsync();
        using var expiredClient = expiredHost.Client();
        var expiredLogin = await expiredHost.LoginAsync(expiredClient, expiredUser.Email!);
        expiredClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expiredLogin.Token);
        Assert.Equal(HttpStatusCode.Unauthorized, (await expiredClient.GetAsync("/api/auth/me")).StatusCode);
    }
}
