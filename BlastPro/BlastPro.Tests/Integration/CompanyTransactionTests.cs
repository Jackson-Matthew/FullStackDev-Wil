using System.Net;
using System.Net.Http.Json;
using BlastPro.Api.Data;
using BlastPro.Api.Models.Dtos;
using BlastPro.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BlastPro.Tests.Integration;

// These tests use isolated SQL Server LocalDB databases, so real rollback and inter-request locking
// are verified. Each host deletes only its unique test database when disposed.
public sealed class CompanyTransactionTests
{
    [Fact]
    public async Task Company_and_main_user_create_without_email_delivery()
    {
        await using var host = new ApiTestHost(sqlServer: true);
        await host.InitializeDatabaseAsync();
        host.AccountMailbox.Available = false;
        using var client = host.Client();
        var request = CompanyTestSetup.Registration();
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/companies", request)).StatusCode);
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Single(await db.Companies.ToListAsync());
            Assert.Single(await db.Users.ToListAsync());
            Assert.Single(await db.UserRoles.ToListAsync());
        }
    }

    [Fact]
    public async Task Company_and_main_user_rollback_when_role_assignment_fails()
    {
        await using var host = new ApiTestHost(sqlServer: true);
        await host.InitializeDatabaseAsync();
        using (var scope = host.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Roles
                .Where(r => r.Name == "MainCompanyUser").ExecuteDeleteAsync();
        using var client = host.Client();
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.PostAsJsonAsync("/api/companies", CompanyTestSetup.Registration())).StatusCode);
        using var verification = host.Services.CreateScope();
        var db = verification.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Empty(await db.Companies.ToListAsync());
        Assert.Empty(await db.Users.ToListAsync());
        Assert.Empty(await db.UserRoles.ToListAsync());
    }

    [Fact]
    public async Task Failed_blaster_delivery_does_not_create_user_or_consume_seat()
    {
        await using var host = new ApiTestHost(sqlServer: true);
        using var main = host.Client();
        await CompanyTestSetup.CreateMainAsync(host, main);
        host.AccountMailbox.FailSend = true;
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await main.PostAsJsonAsync("/api/company/blasters", CompanyTestSetup.BlasterRequest())).StatusCode);
        var company = (await main.GetFromJsonAsync<CompanyDto>("/api/company"))!;
        Assert.Equal(0, company.ActiveBlasterCount);
        Assert.Empty(company.Blasters);
        using var scope = host.Services.CreateScope();
        Assert.Single(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Users.ToListAsync());
    }

    [Fact]
    public async Task Concurrent_creation_and_reactivation_cannot_claim_the_same_last_seat()
    {
        await using var host = new ApiTestHost(sqlServer: true);
        using var main = host.Client();
        await CompanyTestSetup.CreateMainAsync(host, main);
        var first = await CompanyTestSetup.InviteAsync(main);
        for (var i = 0; i < 4; i++) await CompanyTestSetup.InviteAsync(main);
        (await main.PutAsJsonAsync($"/api/company/blasters/{first.Id}/status", new { isActive = false })).EnsureSuccessStatusCode();
        var responses = await Task.WhenAll(
            main.PostAsJsonAsync("/api/company/blasters", CompanyTestSetup.BlasterRequest()),
            main.PostAsJsonAsync("/api/company/blasters", CompanyTestSetup.BlasterRequest()),
            main.PutAsJsonAsync($"/api/company/blasters/{first.Id}/status", new { isActive = true }));
        Assert.Single(responses, r => r.IsSuccessStatusCode);
        Assert.Equal(2, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
        Assert.Equal(5, (await main.GetFromJsonAsync<CompanyDto>("/api/company"))!.ActiveBlasterCount);
        foreach (var response in responses) response.Dispose();
    }
}
