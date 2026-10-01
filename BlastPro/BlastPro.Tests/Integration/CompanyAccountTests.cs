using System.Net;
using System.Net.Http.Json;
using BlastPro.Api.Data;
using BlastPro.Api.Models.Dtos;
using BlastPro.Api.Models.Entities;
using BlastPro.Tests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BlastPro.Tests.Integration;

public sealed class CompanyAccountTests
{
    [Fact]
    public async Task Registration_returns_session_and_allows_immediate_login()
    {
        await using var host = new ApiTestHost();
        await host.InitializeDatabaseAsync();
        using var client = host.Client();
        var request = CompanyTestSetup.Registration();
        var response = await client.PostAsJsonAsync("/api/companies", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var createdSession = (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
        Assert.NotEmpty(createdSession.Token);
        Assert.Equal(new[] { "MainCompanyUser" }, createdSession.Roles);
        var login = await host.LoginAsync(client, request.Email);
        Assert.Equal(new[] { "MainCompanyUser" }, login.Roles);
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(request.RegistrationNumber.ToUpperInvariant(), (await db.Companies.SingleAsync()).RegistrationNumber);
        Assert.Equal(login.CompanyId, (await db.Users.SingleAsync()).CompanyId);
        Assert.True((await db.Users.SingleAsync()).EmailConfirmed);
    }

    [Fact]
    public async Task Invalid_details_duplicate_registration_and_duplicate_email_are_rejected()
    {
        await using var host = new ApiTestHost();
        await host.InitializeDatabaseAsync();
        using var client = host.Client();
        var weak = CompanyTestSetup.Registration();
        weak.Password = "weakpassword";
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/companies", weak)).StatusCode);
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Empty(await db.Companies.ToListAsync());
            Assert.Empty(await db.Users.ToListAsync());
        }
        weak.Password = ApiTestHost.Password;
        weak.ContactEmail = "invalid";
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/companies", weak)).StatusCode);
        var request = await CompanyTestSetup.CreateMainAsync(host, client);
        var duplicate = CompanyTestSetup.Registration();
        duplicate.RegistrationNumber = " " + request.RegistrationNumber.ToLowerInvariant() + " ";
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/companies", duplicate)).StatusCode);
        duplicate.RegistrationNumber = Guid.NewGuid().ToString();
        duplicate.Email = request.Email.ToUpperInvariant();
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/companies", duplicate)).StatusCode);
    }

    [Fact]
    public async Task Invitation_has_no_password_until_blaster_accepts_and_cannot_be_replayed()
    {
        await using var host = new ApiTestHost();
        using var main = host.Client();
        await CompanyTestSetup.CreateMainAsync(host, main);
        var blaster = await CompanyTestSetup.InviteAsync(main);
        using (var scope = host.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = (await users.FindByIdAsync(blaster.Id))!;
            Assert.False(await users.HasPasswordAsync(user));
            Assert.False(user.EmailConfirmed);
            Assert.True(user.IsActive);
            Assert.Equal(new[] { "Blaster" }, await users.GetRolesAsync(user));
            Assert.Equal((await main.GetFromJsonAsync<CompanyDto>("/api/company"))!.Id, user.CompanyId);
        }
        using var anonymous = host.Client();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/auth/login", new { blaster.Email, password = ApiTestHost.Password })).StatusCode);
        await anonymous.PostAsJsonAsync("/api/auth/forgot-password", new { blaster.Email });
        Assert.False(host.Mailbox.Messages.ContainsKey(blaster.Email));
        var token = host.AccountMailbox.Invitations[blaster.Email];
        Assert.Equal(HttpStatusCode.BadRequest, (await anonymous.PostAsJsonAsync("/api/auth/accept-invitation", new { blaster.Email, token, password = "weakpassword" })).StatusCode);
        using var accepted = await CompanyTestSetup.AcceptAsync(host, blaster);
        Assert.Equal(HttpStatusCode.BadRequest, (await anonymous.PostAsJsonAsync("/api/auth/accept-invitation", new { blaster.Email, token, password = "ChangedPassword123!" })).StatusCode);
    }

    [Fact]
    public async Task Sixth_active_blaster_and_reactivation_at_capacity_are_rejected()
    {
        await using var host = new ApiTestHost();
        using var main = host.Client();
        await CompanyTestSetup.CreateMainAsync(host, main);
        var first = await CompanyTestSetup.InviteAsync(main);
        for (var i = 0; i < 4; i++) await CompanyTestSetup.InviteAsync(main);
        Assert.Equal(HttpStatusCode.Conflict, (await main.PostAsJsonAsync("/api/company/blasters", CompanyTestSetup.BlasterRequest())).StatusCode);
        Assert.Equal(5, (await main.GetFromJsonAsync<CompanyDto>("/api/company"))!.ActiveBlasterCount);
        Assert.Equal(HttpStatusCode.BadRequest, (await main.PutAsJsonAsync($"/api/company/blasters/{first.Id}/status", new { })).StatusCode);
        Assert.True((await main.GetFromJsonAsync<BlasterDto>($"/api/company/blasters/{first.Id}"))!.IsActive);
        (await main.PutAsJsonAsync($"/api/company/blasters/{first.Id}/status", new { isActive = false })).EnsureSuccessStatusCode();
        Assert.Equal(4, (await main.GetFromJsonAsync<CompanyDto>("/api/company"))!.ActiveBlasterCount);
        var replacement = await CompanyTestSetup.InviteAsync(main);
        Assert.Equal(HttpStatusCode.Conflict, (await main.PutAsJsonAsync($"/api/company/blasters/{first.Id}/status", new { isActive = true })).StatusCode);
        (await main.PutAsJsonAsync($"/api/company/blasters/{replacement.Id}/status", new { isActive = false })).EnsureSuccessStatusCode();
        (await main.PutAsJsonAsync($"/api/company/blasters/{first.Id}/status", new { isActive = true })).EnsureSuccessStatusCode();
        Assert.Equal(5, (await main.GetFromJsonAsync<CompanyDto>("/api/company"))!.ActiveBlasterCount);
    }

    [Fact]
    public async Task Deactivation_retains_designs_and_revokes_old_session_after_reactivation()
    {
        await using var host = new ApiTestHost();
        using var main = host.Client();
        await CompanyTestSetup.CreateMainAsync(host, main);
        var blaster = await CompanyTestSetup.InviteAsync(main);
        using var blasterClient = await CompanyTestSetup.AcceptAsync(host, blaster);
        (await blasterClient.PostAsJsonAsync("/api/projects", new { name = "Kept design", siteLocation = "Test site", blastType = "Surface" })).EnsureSuccessStatusCode();
        (await main.PutAsJsonAsync($"/api/company/blasters/{blaster.Id}/status", new { isActive = false })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Unauthorized, (await blasterClient.GetAsync("/api/projects")).StatusCode);
        Assert.Single((await main.GetFromJsonAsync<List<ProjectSummaryDto>>("/api/projects"))!);
        (await main.PutAsJsonAsync($"/api/company/blasters/{blaster.Id}/status", new { isActive = true })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Unauthorized, (await blasterClient.GetAsync("/api/projects")).StatusCode);
        await host.LoginAsync(host.Client(), blaster.Email);
    }

    [Fact]
    public async Task Resent_invitation_revokes_previous_token_and_inactive_invite_is_rejected()
    {
        await using var host = new ApiTestHost();
        using var main = host.Client();
        await CompanyTestSetup.CreateMainAsync(host, main);
        var blaster = await CompanyTestSetup.InviteAsync(main);
        var oldToken = host.AccountMailbox.Invitations[blaster.Email];
        (await main.PostAsJsonAsync($"/api/company/blasters/{blaster.Id}/invitation", new { })).EnsureSuccessStatusCode();
        using var client = host.Client();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/accept-invitation", new { blaster.Email, token = oldToken, password = ApiTestHost.Password })).StatusCode);
        var newToken = host.AccountMailbox.Invitations[blaster.Email];
        (await main.PutAsJsonAsync($"/api/company/blasters/{blaster.Id}/status", new { isActive = false })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/accept-invitation", new { blaster.Email, token = newToken, password = ApiTestHost.Password })).StatusCode);
    }

    [Fact]
    public async Task Company_registration_does_not_depend_on_email_delivery()
    {
        await using var host = new ApiTestHost();
        await host.InitializeDatabaseAsync();
        using var client = host.Client();
        var request = CompanyTestSetup.Registration();
        host.AccountMailbox.Available = false;
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/companies", request)).StatusCode);
        Assert.Empty(host.AccountMailbox.Invitations);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/auth/confirm-email", new { request.Email, token = "unused" })).StatusCode);
    }

    [Fact]
    public async Task Invitation_tokens_expire()
    {
        await using var host = new ApiTestHost(resetLifetime: TimeSpan.FromMilliseconds(1));
        await host.InitializeDatabaseAsync();
        using var client = host.Client();
        var registration = CompanyTestSetup.Registration();
        (await client.PostAsJsonAsync("/api/companies", registration)).EnsureSuccessStatusCode();
        var login = await host.LoginAsync(client, registration.Email);
        client.DefaultRequestHeaders.Authorization = new("Bearer", login.Token);
        var blaster = await CompanyTestSetup.InviteAsync(client);
        await Task.Delay(25);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/accept-invitation", new { blaster.Email, token = host.AccountMailbox.Invitations[blaster.Email], password = ApiTestHost.Password })).StatusCode);
    }

    [Fact]
    public async Task Existing_unconfirmed_main_user_can_sign_in_and_reset_password()
    {
        await using var host = new ApiTestHost();
        await host.InitializeDatabaseAsync();
        using var client = host.Client();
        var registration = CompanyTestSetup.Registration();
        (await client.PostAsJsonAsync("/api/companies", registration)).EnsureSuccessStatusCode();
        using (var scope = host.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = (await users.FindByEmailAsync(registration.Email))!;
            user.EmailConfirmed = false;
            Assert.True((await users.UpdateAsync(user)).Succeeded);
        }
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/auth/login", new { registration.Email, registration.Password })).StatusCode);
        string token;
        using (var scope = host.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            token = BlastPro.Api.Services.PasswordResetTokens.Encode(await users.GeneratePasswordResetTokenAsync((await users.FindByEmailAsync(registration.Email))!));
        }
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/auth/reset-password", new
            { registration.Email, token, newPassword = "ChangedPassword123!" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/auth/login", new
            { registration.Email, password = "ChangedPassword123!" })).StatusCode);
    }

    [Fact]
    public async Task Inactive_company_revokes_sessions_and_rejects_login_for_both_roles()
    {
        await using var host = new ApiTestHost();
        using var main = host.Client();
        var registration = await CompanyTestSetup.CreateMainAsync(host, main);
        var blaster = await CompanyTestSetup.InviteAsync(main);
        using var blasterClient = await CompanyTestSetup.AcceptAsync(host, blaster);
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var company = await db.Companies.SingleAsync();
            company.IsActive = false;
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await main.GetAsync("/api/company")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await blasterClient.GetAsync("/api/profile")).StatusCode);
        foreach (var email in new[] { registration.Email, blaster.Email })
            Assert.Equal(HttpStatusCode.Unauthorized, (await main.PostAsJsonAsync("/api/auth/login", new { email, password = ApiTestHost.Password })).StatusCode);
    }

}
