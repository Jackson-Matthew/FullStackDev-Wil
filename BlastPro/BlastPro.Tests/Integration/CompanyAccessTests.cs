
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

public sealed class CompanyAccessTests
{
    [Fact]
    public async Task Blasters_cannot_open_or_mutate_company_administration()
    {
        await using var host = new ApiTestHost();
        using var main = host.Client();
        await CompanyTestSetup.CreateMainAsync(host, main);
        var blaster = await CompanyTestSetup.InviteAsync(main);
        using var client = await CompanyTestSetup.AcceptAsync(host, blaster);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/company")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync("/api/company", new { contactEmail = "office@example.test", contactPhone = "+27 11 555 1234" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/company/blasters", CompanyTestSetup.BlasterRequest())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/company/blasters/{blaster.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync($"/api/company/blasters/{blaster.Id}/status", new { isActive = false })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync($"/api/company/blasters/{blaster.Id}/invitation", new { })).StatusCode);
    }

    [Fact]
    public async Task Main_users_cannot_read_update_deactivate_or_invite_another_companys_blasters()
    {
        await using var host = new ApiTestHost();
        using var companyA = host.Client();
        using var companyB = host.Client();
        await CompanyTestSetup.CreateMainAsync(host, companyA);
        await CompanyTestSetup.CreateMainAsync(host, companyB);
        var blaster = await CompanyTestSetup.InviteAsync(companyB);
        Assert.Equal(HttpStatusCode.NotFound, (await companyA.GetAsync($"/api/company/blasters/{blaster.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await companyA.PutAsJsonAsync($"/api/company/blasters/{blaster.Id}", new { fullName = "Changed", phoneNumber = "+27 82 555 1234", certificationId = "Changed" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await companyA.PutAsJsonAsync($"/api/company/blasters/{blaster.Id}/status", new { isActive = false })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await companyA.PostAsJsonAsync($"/api/company/blasters/{blaster.Id}/invitation", new { })).StatusCode);
        Assert.Empty((await companyA.GetFromJsonAsync<CompanyDto>("/api/company"))!.Blasters);
    }

    [Fact]
    public async Task Dashboard_projects_results_and_report_data_respect_company_and_owner()
    {
        await using var host = new ApiTestHost();
        using var main = host.Client();
        using var outsiderMain = host.Client();
        await CompanyTestSetup.CreateMainAsync(host, main);
        await CompanyTestSetup.CreateMainAsync(host, outsiderMain);
        var first = await CompanyTestSetup.InviteAsync(main);
        var second = await CompanyTestSetup.InviteAsync(main);
        using var firstClient = await CompanyTestSetup.AcceptAsync(host, first);
        using var secondClient = await CompanyTestSetup.AcceptAsync(host, second);
        var firstProject = await CreateProject(firstClient, "First design");
        var secondProject = await CreateProject(secondClient, "Second design");
        var outsideProject = await CreateProject(outsiderMain, "Outside design");
        Assert.Equal(2, (await main.GetFromJsonAsync<List<ProjectSummaryDto>>("/api/projects"))!.Count);
        Assert.Equal(firstProject, Assert.Single((await firstClient.GetFromJsonAsync<List<ProjectSummaryDto>>("/api/projects"))!).Id);
        foreach (var path in new[] { $"/api/projects/{firstProject}", $"/api/projects/{firstProject}/pattern-design", $"/api/projects/{firstProject}/results" })
        {
            Assert.Equal(HttpStatusCode.OK, (await main.GetAsync(path)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await secondClient.GetAsync(path)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await outsiderMain.GetAsync(path)).StatusCode);
        }
        Assert.Equal(HttpStatusCode.NotFound, (await main.GetAsync($"/api/projects/{outsideProject}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await firstClient.GetAsync($"/api/projects/{secondProject}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await outsiderMain.GetAsync($"/api/projects/{firstProject}/results/123")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await outsiderMain.PostAsJsonAsync($"/api/projects/{firstProject}/calculations", new { })).StatusCode);
    }

    [Fact]
    public async Task Profile_and_admin_updates_only_change_permitted_fields()
    {
        await using var host = new ApiTestHost();
        using var main = host.Client();
        var registration = await CompanyTestSetup.CreateMainAsync(host, main);
        var blaster = await CompanyTestSetup.InviteAsync(main);
        using var client = await CompanyTestSetup.AcceptAsync(host, blaster);
        var company = (await main.GetFromJsonAsync<CompanyDto>("/api/company"))!;
        (await client.PutAsJsonAsync("/api/profile", new
        {
            fullName = "Updated Blaster", nickName = "Sam", phoneNumber = "+27 82 555 4321", country = "South Africa",
            companyId = 9999, role = "MainCompanyUser", isActive = false, email = "stolen@example.test", certificationId = "FORGED", id = registration.Email
        })).EnsureSuccessStatusCode();
        var profile = (await client.GetFromJsonAsync<ProfileDto>("/api/profile"))!;
        Assert.Equal("Updated Blaster", profile.FullName);
        Assert.Equal(blaster.Email, profile.Email);
        Assert.Equal(blaster.CertificationId, profile.CertificationId);
        Assert.Equal(new[] { "Blaster" }, profile.Roles);
        (await main.PutAsJsonAsync("/api/company", new
        {
            contactEmail = "updated@example.test", contactPhone = "+27 11 555 4321", address = "Pretoria",
            id = 9999, name = "Forged", registrationNumber = "Forged", isActive = false
        })).EnsureSuccessStatusCode();
        var updated = (await main.GetFromJsonAsync<CompanyDto>("/api/company"))!;
        Assert.Equal(company.Id, updated.Id);
        Assert.Equal(company.Name, updated.Name);
        Assert.Equal(company.RegistrationNumber, updated.RegistrationNumber);
        Assert.Equal("updated@example.test", updated.ContactEmail);
        (await main.PutAsJsonAsync($"/api/company/blasters/{blaster.Id}", new
        {
            fullName = "Admin Updated", phoneNumber = "+27 82 555 9999", certificationId = "NEW-CERT",
            companyId = 9999, role = "MainCompanyUser", isActive = false, email = "stolen@example.test"
        })).EnsureSuccessStatusCode();
        using var scope = host.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = (await users.FindByIdAsync(blaster.Id))!;
        Assert.Equal(company.Id, user.CompanyId);
        Assert.True(user.IsActive);
        Assert.Equal(blaster.Email, user.Email);
        Assert.Equal("NEW-CERT", user.CertificationId);
        Assert.Equal(new[] { "Blaster" }, await users.GetRolesAsync(user));
        Assert.Equal(HttpStatusCode.NotFound, (await main.PutAsJsonAsync("/api/profile/another-user", new { fullName = "Forged" })).StatusCode);
    }

    [Fact]
    public async Task Blaster_email_is_globally_unique_and_certification_is_unique_within_company()
    {
        await using var host = new ApiTestHost();
        using var main = host.Client();
        using var other = host.Client();
        await CompanyTestSetup.CreateMainAsync(host, main);
        await CompanyTestSetup.CreateMainAsync(host, other);
        var first = await CompanyTestSetup.InviteAsync(main);
        Assert.Equal(HttpStatusCode.Conflict, (await other.PostAsJsonAsync("/api/company/blasters", CompanyTestSetup.BlasterRequest(first.Email.ToUpperInvariant()))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await main.PostAsJsonAsync("/api/company/blasters", CompanyTestSetup.BlasterRequest(certification: first.CertificationId))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await other.PostAsJsonAsync("/api/company/blasters", CompanyTestSetup.BlasterRequest(certification: first.CertificationId))).StatusCode);
        var second = await CompanyTestSetup.InviteAsync(main);
        Assert.Equal(HttpStatusCode.Conflict, (await main.PutAsJsonAsync($"/api/company/blasters/{second.Id}", new { second.FullName, second.PhoneNumber, first.CertificationId })).StatusCode);
    }

    private static async Task<int> CreateProject(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/projects", new { name, siteLocation = "Test site", blastType = "Surface" });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProjectDetailDto>())!.Id;
    }
}
