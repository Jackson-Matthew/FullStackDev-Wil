using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BlastPro.Api.Data;
using BlastPro.Api.Models.Dtos;
using BlastPro.Api.Models.Entities;
using BlastPro.Tests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace BlastPro.Tests.Integration;

public sealed class CalculationResultsTests
{
    [Fact]
    public void Results_migration_generates_the_expected_sql()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=localhost;Database=BlastProMigrationCheck;Trusted_Connection=True;")
            .Options;
        using var db = new ApplicationDbContext(options);
        var script = db.GetService<IMigrator>().GenerateScript(
            "20260919204912_InitialCreate", "20260924220000_SaveCalculationResults");

        Assert.Contains("CurrencyCode", script);
        Assert.Contains("CREATE UNIQUE INDEX", script);
        Assert.Contains("[IsCurrent] = 1", script);
    }

    [Fact]
    public async Task Missing_results_return_an_empty_state()
    {
        await using var host = new ApiTestHost();
        var user = await host.AddUserAsync();
        var projectId = await SeedProject(host, user);
        using var client = await AuthenticatedClient(host, user);

        var page = await client.GetFromJsonAsync<ProjectResultsDto>($"/api/projects/{projectId}/results");

        Assert.NotNull(page);
        Assert.Equal("Results Project", page.ProjectName);
        Assert.False(page.HasResults);
        Assert.Null(page.Result);
        Assert.Empty(page.History);
    }

    [Fact]
    public async Task Calculation_uses_saved_holes_and_products_and_preserves_history()
    {
        await using var host = new ApiTestHost();
        var user = await host.AddUserAsync();
        var projectId = await SeedProject(host, user);
        using var client = await AuthenticatedClient(host, user);

        var firstResponse = await client.PostAsJsonAsync($"/api/projects/{projectId}/calculations", new
        {
            delayWindowMilliseconds = 50,
            holes = new[] { new { chargeKg = 9999 } },
            products = new[] { new { pricePerKg = 0 } }
        });
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        var first = await firstResponse.Content.ReadFromJsonAsync<CalculationResultDto>();
        Assert.NotNull(first);
        Assert.Equal(12m, first.TotalExplosiveKg);
        Assert.Equal(120m, first.TotalCost);
        Assert.Equal("ZAR", first.CurrencyCode);
        Assert.Null(first.EstimatedVolumeCubicMetres);
        Assert.Null(first.PowderFactorKgPerTonne);
        Assert.Contains(first.Warnings, w => w.Code == "VOLUME_UNAVAILABLE");

        var secondResponse = await client.PostAsJsonAsync($"/api/projects/{projectId}/calculations", new
        {
            delayWindowMilliseconds = 50,
            subdrillMetres = 1m
        });
        Assert.Equal(HttpStatusCode.Created, secondResponse.StatusCode);
        var second = await secondResponse.Content.ReadFromJsonAsync<CalculationResultDto>();
        Assert.NotNull(second);
        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(120m, second.TotalCost);
        Assert.NotNull(second.EstimatedVolumeCubicMetres);

        var page = await client.GetFromJsonAsync<ProjectResultsDto>($"/api/projects/{projectId}/results");
        Assert.NotNull(page);
        Assert.True(page.HasResults);
        Assert.False(page.IsOutdated);
        Assert.Equal(second.Id, page.Result!.Id);
        Assert.Single(page.History);
        Assert.Equal(first.Id, page.History[0].Id);
        Assert.Equal(first.Warnings.Count, page.History[0].WarningCount);

        var old = await client.GetFromJsonAsync<CalculationResultDto>(
            $"/api/projects/{projectId}/results/{first.Id}");
        Assert.NotNull(old);
        Assert.True(old.IsOutdated);
        Assert.Equal(first.Warnings.Count, old.Warnings.Count);

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(2, await db.CalculationResults.CountAsync(r => r.BlastProjectId == projectId));
        Assert.Single(await db.CalculationResults.Where(r => r.BlastProjectId == projectId && r.IsCurrent).ToListAsync());
        Assert.Equal(first.Warnings.Count + second.Warnings.Count,
            await db.BlastWarnings.CountAsync(w => w.CalculationResult.BlastProjectId == projectId));
    }

    [Fact]
    public async Task Unavailable_cost_stays_null_and_invalid_settings_save_nothing()
    {
        await using var host = new ApiTestHost();
        var user = await host.AddUserAsync();
        var projectId = await SeedProject(host, user, selectProduct: false);
        using var client = await AuthenticatedClient(host, user);

        var invalid = await client.PostAsJsonAsync($"/api/projects/{projectId}/calculations", new
        {
            delayWindowMilliseconds = 0,
            receptorDistanceMetres = -1m
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        var valid = await client.PostAsJsonAsync($"/api/projects/{projectId}/calculations", new
        {
            delayWindowMilliseconds = 50
        });
        Assert.Equal(HttpStatusCode.Created, valid.StatusCode);
        var result = await valid.Content.ReadFromJsonAsync<CalculationResultDto>();
        Assert.NotNull(result);
        Assert.Null(result.TotalCost);
        Assert.Null(result.CurrencyCode);
        Assert.Contains(result.Warnings, w => w.Code == "COST_UNAVAILABLE");

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Single(await db.CalculationResults.Where(r => r.BlastProjectId == projectId).ToListAsync());
    }

    [Fact]
    public async Task Saving_changed_pattern_marks_the_saved_result_outdated()
    {
        await using var host = new ApiTestHost();
        var user = await host.AddUserAsync();
        var projectId = await SeedProject(host, user);
        using var client = await AuthenticatedClient(host, user);
        (await client.PostAsJsonAsync($"/api/projects/{projectId}/calculations",
            new { delayWindowMilliseconds = 50 })).EnsureSuccessStatusCode();

        var design = await client.GetFromJsonAsync<PatternDesignDto>($"/api/projects/{projectId}/pattern-design");
        Assert.NotNull(design);
        var saved = await client.PutAsJsonAsync($"/api/projects/{projectId}/pattern-design", new
        {
            rockType = "Granite", rockDensity = 2.5m, burden = 3m, spacing = 4m,
            vibrationThreshold = 10m, rowVersion = design.RowVersion,
            holes = design.Holes.Select(h => new
            {
                id = h.Id, number = h.Number, x = h.X, y = h.Y, depth = h.Depth,
                explosiveProductId = h.ExplosiveProductId,
                charge = h.Charge + 1m, stemming = h.Stemming, delay = h.Delay
            }).ToArray()
        });
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);

        var page = await client.GetFromJsonAsync<ProjectResultsDto>($"/api/projects/{projectId}/results");
        Assert.NotNull(page);
        Assert.True(page.HasResults);
        Assert.True(page.IsOutdated);
        Assert.False(page.Result!.IsCurrent);
    }

    [Fact]
    public async Task Other_company_and_other_blaster_cannot_read_or_calculate()
    {
        await using var host = new ApiTestHost();
        var owner = await host.AddUserAsync();
        var projectId = await SeedProject(host, owner);
        var outsider = await host.AddUserAsync();
        using var crossCompany = await AuthenticatedClient(host, outsider);
        Assert.Equal(HttpStatusCode.NotFound,
            (await crossCompany.GetAsync($"/api/projects/{projectId}/results")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await crossCompany.PostAsJsonAsync($"/api/projects/{projectId}/calculations",
                new { delayWindowMilliseconds = 50 })).StatusCode);

        ApplicationUser colleague;
        using (var scope = host.Services.CreateScope())
        {
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            colleague = new ApplicationUser
            {
                UserName = "colleague@example.test", Email = "colleague@example.test",
                FullName = "Colleague", CompanyId = owner.CompanyId, IsActive = true,
                EmailConfirmed = true
            };
            Assert.True((await manager.CreateAsync(colleague, ApiTestHost.Password)).Succeeded);
            Assert.True((await manager.AddToRoleAsync(colleague, "Blaster")).Succeeded);
        }
        using var sameCompany = await AuthenticatedClient(host, colleague);
        Assert.Equal(HttpStatusCode.NotFound,
            (await sameCompany.GetAsync($"/api/projects/{projectId}/results")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await sameCompany.PostAsJsonAsync($"/api/projects/{projectId}/calculations",
                new { delayWindowMilliseconds = 50 })).StatusCode);
    }

    private static async Task<HttpClient> AuthenticatedClient(ApiTestHost host, ApplicationUser user)
    {
        var client = host.Client();
        var token = (await host.LoginAsync(client, user.Email!)).Token;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<int> SeedProject(ApiTestHost host, ApplicationUser user, bool selectProduct = true)
    {
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var product = new ExplosiveProduct
        {
            CompanyId = user.CompanyId, Name = "Test explosive", PricePerKg = 10m,
            CurrencyCode = "ZAR", IsActive = true
        };
        db.ExplosiveProducts.Add(product);
        await db.SaveChangesAsync();
        var project = new BlastProject
        {
            CompanyId = user.CompanyId, OwnerId = user.Id,
            Name = "Results Project", SiteLocation = "Test site", BlastType = "Surface",
            RockType = "Granite", RockDensity = 2.5m, Burden = 3m, Spacing = 4m,
            VibrationThreshold = 10m,
            ExplosiveProductId = selectProduct ? product.Id : null
        };
        db.BlastProjects.Add(project);
        await db.SaveChangesAsync();
        db.BlastHoles.AddRange(
            new BlastHole { BlastProjectId = project.Id, HoleNumber = 1, Depth = 10m, ChargeKg = 5m, StemmingMetres = 2m, DelayMilliseconds = 0 },
            new BlastHole { BlastProjectId = project.Id, HoleNumber = 2, Depth = 11m, ChargeKg = 7m, StemmingMetres = 2m, DelayMilliseconds = 25 });
        await db.SaveChangesAsync();
        return project.Id;
    }
}
