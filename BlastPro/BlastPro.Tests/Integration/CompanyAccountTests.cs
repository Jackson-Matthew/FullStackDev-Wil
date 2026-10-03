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
    public async Task Created_blaster_can_sign_in_immediately_without_an_invitation()
    {
        await using var host = new ApiTestHost();
        using var main = host.Client();
        await CompanyTestSetup.CreateMainAsync(host, main);
        var blaster = await CompanyTestSetup.CreateBlasterAsync(main);
        using (var scope = host.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = (await users.FindByIdAsync(blaster.Id))!;
            Assert.True(await users.HasPasswordAsync(user));
            Assert.True(user.EmailConfirmed);
            Assert.True(user.IsActive);
            Assert.Equal(new[] { "Blaster" }, await users.GetRolesAsync(user));
            Assert.Equal((await main.GetFromJsonAsync<CompanyDto>("/api/company"))!.Id, user.CompanyId);
        }
        using var anonymous = host.Client();
        Assert.Equal(HttpStatusCode.OK, (await anonymous.PostAsJsonAsync("/api/auth/login", new { blaster.Email, password = ApiTestHost.Password })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.PostAsJsonAsync("/api/auth/accept-invitation", new { blaster.Email, token = "unused", password = ApiTestHost.Password })).StatusCode);
    }

    [Fact]
    public async Task Main_user_can_set_a_blaster_password_without_email_delivery()
    {
        await using var host = new ApiTestHost();
        using var main = host.Client();
        await CompanyTestSetup.CreateMainAsync(host, main);
        var blaster = await CompanyTestSetup.CreateBlasterAsync(main);
        using var oldSession = await CompanyTestSetup.SignInBlasterAsync(host, blaster);
        Assert.Equal(HttpStatusCode.NoContent, (await main.PutAsJsonAsync(
            $"/api/company/blasters/{blaster.Id}/password", new { password = "NewPassword123!" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await oldSession.GetAsync("/api/projects")).StatusCode);
        using var anonymous = host.Client();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/auth/login",
            new { blaster.Email, password = ApiTestHost.Password })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anonymous.PostAsJsonAsync("/api/auth/login",
            new { blaster.Email, password = "NewPassword123!" })).StatusCode);

        using (var scope = host.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = (await users.FindByIdAsync(blaster.Id))!;
            Assert.True((await users.RemovePasswordAsync(user)).Succeeded);
            user.EmailConfirmed = false;
            Assert.True((await users.UpdateAsync(user)).Succeeded);
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/auth/login",
            new { blaster.Email, password = "NewPassword123!" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await main.PutAsJsonAsync(
            $"/api/company/blasters/{blaster.Id}/password", new { password = "RecoveredPassword123!" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anonymous.PostAsJsonAsync("/api/auth/login",
            new { blaster.Email, password = "RecoveredPassword123!" })).StatusCode);
    }

    [Fact]
    public async Task Sixth_active_blaster_and_reactivation_at_capacity_are_rejected()
    {
        await using var host = new ApiTestHost();
        using var main = host.Client();
        await CompanyTestSetup.CreateMainAsync(host, main);
        var first = await CompanyTestSetup.CreateBlasterAsync(main);
        for (var i = 0; i < 4; i++) await CompanyTestSetup.CreateBlasterAsync(main);
        Assert.Equal(HttpStatusCode.Conflict, (await main.PostAsJsonAsync("/api/company/blasters", CompanyTestSetup.BlasterRequest())).StatusCode);
        Assert.Equal(5, (await main.GetFromJsonAsync<CompanyDto>("/api/company"))!.ActiveBlasterCount);
        Assert.Equal(HttpStatusCode.BadRequest, (await main.PutAsJsonAsync($"/api/company/blasters/{first.Id}/status", new { })).StatusCode);
        Assert.True((await main.GetFromJsonAsync<BlasterDto>($"/api/company/blasters/{first.Id}"))!.IsActive);
        (await main.PutAsJsonAsync($"/api/company/blasters/{first.Id}/status", new { isActive = false })).EnsureSuccessStatusCode();
        Assert.Equal(4, (await main.GetFromJsonAsync<CompanyDto>("/api/company"))!.ActiveBlasterCount);
        var replacement = await CompanyTestSetup.CreateBlasterAsync(main);
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
        var blaster = await CompanyTestSetup.CreateBlasterAsync(main);
        using var blasterClient = await CompanyTestSetup.SignInBlasterAsync(host, blaster);
        (await blasterClient.PostAsJsonAsync("/api/projects", new { name = "Kept design", siteLocation = "Test site", blastType = "Surface" })).EnsureSuccessStatusCode();
        (await main.PutAsJsonAsync($"/api/company/blasters/{blaster.Id}/status", new { isActive = false })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Unauthorized, (await blasterClient.GetAsync("/api/projects")).StatusCode);
        Assert.Single((await main.GetFromJsonAsync<List<ProjectSummaryDto>>("/api/projects"))!);
        (await main.PutAsJsonAsync($"/api/company/blasters/{blaster.Id}/status", new { isActive = true })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Unauthorized, (await blasterClient.GetAsync("/api/projects")).StatusCode);
        await host.LoginAsync(host.Client(), blaster.Email);
    }

    [Fact]
    public async Task Weak_password_does_not_create_a_blaster_or_use_a_seat()
    {
        await using var host = new ApiTestHost();
        using var main = host.Client();
        await CompanyTestSetup.CreateMainAsync(host, main);
        Assert.Equal(HttpStatusCode.BadRequest, (await main.PostAsJsonAsync("/api/company/blasters", new
        {
            email = $"weak-{Guid.NewGuid():N}@example.test", password = "weakpassword"
        })).StatusCode);
        var company = (await main.GetFromJsonAsync<CompanyDto>("/api/company"))!;
        Assert.Empty(company.Blasters);
        Assert.Equal(0, company.ActiveBlasterCount);
    }

    [Fact]
    public async Task Company_registration_has_no_email_confirmation_step()
    {
        await using var host = new ApiTestHost();
        await host.InitializeDatabaseAsync();
        using var client = host.Client();
        var request = CompanyTestSetup.Registration();
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/companies", request)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/auth/confirm-email", new { request.Email, token = "unused" })).StatusCode);
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
        var blaster = await CompanyTestSetup.CreateBlasterAsync(main);
        using var blasterClient = await CompanyTestSetup.SignInBlasterAsync(host, blaster);
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
