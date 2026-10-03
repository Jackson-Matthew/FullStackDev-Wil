using System.Net;
using System.Net.Http.Json;
using BlastPro.Api.Data;
using BlastPro.Api.Models.Dtos;
using BlastPro.Api.Models.Entities;
using BlastPro.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BlastPro.Tests.Integration;

public sealed class ProductAdministrationTests
{
    [Fact]
    public async Task Products_are_company_scoped_and_saved_costs_keep_their_original_price()
    {
        await using var host = new ApiTestHost();
        using var companyA = host.Client();
        using var companyB = host.Client();
        await CompanyTestSetup.CreateMainAsync(host, companyA);
        await CompanyTestSetup.CreateMainAsync(host, companyB);
        var blaster = await CompanyTestSetup.CreateBlasterAsync(companyA);
        using var blasterClient = await CompanyTestSetup.SignInBlasterAsync(host, blaster);

        var created = await companyA.PostAsJsonAsync("/api/company/products",
            new { name = "Bulk emulsion", pricePerKg = 35m, isActive = true });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var product = (await created.Content.ReadFromJsonAsync<ProductDto>())!;

        Assert.Single((await companyA.GetFromJsonAsync<List<ProductDto>>("/api/company/products"))!);
        Assert.Empty((await companyB.GetFromJsonAsync<List<ProductDto>>("/api/company/products"))!);
        Assert.Equal(HttpStatusCode.Forbidden, (await blasterClient.GetAsync("/api/company/products")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await blasterClient.PostAsJsonAsync("/api/company/products",
            new { name = "Other", pricePerKg = 1m })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await companyB.PutAsJsonAsync($"/api/company/products/{product.Id}",
            new { name = "Stolen", pricePerKg = 1m, isActive = true })).StatusCode);

        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var saved = new CalculationResult
            {
                BlastProject = new BlastProject
                {
                    Name = "Saved cost", SiteLocation = "Site", BlastType = "Surface",
                    CompanyId = (await db.ExplosiveProducts.SingleAsync(p => p.Id == product.Id)).CompanyId,
                    OwnerId = (await db.Users.SingleAsync(u => u.Email == blaster.Email)).Id
                },
                CalculatedByUserId = (await db.Users.SingleAsync(u => u.Email == blaster.Email)).Id,
                TotalCost = 350m, CurrencyCode = "ZAR", CalculatedAtUtc = DateTime.UtcNow
            };
            db.CalculationResults.Add(saved);
            await db.SaveChangesAsync();
        }

        Assert.Equal(HttpStatusCode.NoContent, (await companyA.PutAsJsonAsync($"/api/company/products/{product.Id}",
            new { name = "Bulk emulsion", pricePerKg = 50m, isActive = false })).StatusCode);
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(350m, (await db.CalculationResults.SingleAsync()).TotalCost);
            Assert.Equal(50m, (await db.ExplosiveProducts.SingleAsync(p => p.Id == product.Id)).PricePerKg);
        }
    }

    [Fact]
    public async Task Sample_prices_are_idempotent_and_invalid_prices_are_rejected()
    {
        await using var host = new ApiTestHost();
        using var client = host.Client();
        await CompanyTestSetup.CreateMainAsync(host, client);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/company/products",
            new { name = "Invalid", pricePerKg = -1m })).StatusCode);
        (await client.PostAsJsonAsync("/api/company/products/samples", new { })).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/company/products/samples", new { })).EnsureSuccessStatusCode();
        var products = (await client.GetFromJsonAsync<List<ProductDto>>("/api/company/products"))!;
        Assert.Equal(3, products.Count);
        Assert.Contains(products, p => p.AeciProductCode == "S100" && p.PricePerKg == 35m);
        Assert.Contains(products, p => p.AeciProductCode == "S300" && p.PricePerKg == 28m);
        Assert.Contains(products, p => p.AeciProductCode == "PG-ECO" && p.PricePerKg == 55m);
    }

    [Fact]
    public async Task Aeci_price_applies_to_calculations_and_saved_results_keep_the_original_cost()
    {
        await using var host = new ApiTestHost();
        using var client = host.Client();
        await CompanyTestSetup.CreateMainAsync(host, client);
        (await client.PostAsJsonAsync("/api/company/products/samples", new { })).EnsureSuccessStatusCode();
        var product = (await client.GetFromJsonAsync<List<ProductDto>>("/api/company/products"))!
            .Single(p => p.AeciProductCode == "S100");

        int projectId;
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var companyId = (await db.ExplosiveProducts.SingleAsync(p => p.Id == product.Id)).CompanyId;
            var ownerId = (await db.Users.SingleAsync(u => u.CompanyId == companyId)).Id;
            var project = new BlastProject
            {
                CompanyId = companyId, OwnerId = ownerId, Name = "Priced pattern",
                SiteLocation = "Test site", BlastType = "Surface", RockType = "Granite",
                RockDensity = 2.5m, Burden = 3m, Spacing = 4m, VibrationThreshold = 10m
            };
            db.BlastProjects.Add(project);
            await db.SaveChangesAsync();
            db.BlastHoles.Add(new BlastHole
            {
                BlastProjectId = project.Id, HoleNumber = 1, Depth = 10m,
                ChargeKg = 5m, StemmingMetres = 2m, AeciProductCode = "S100"
            });
            await db.SaveChangesAsync();
            projectId = project.Id;
        }

        var firstResponse = await client.PostAsJsonAsync($"/api/projects/{projectId}/calculations",
            new { delayWindowMilliseconds = 50 });
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        var first = (await firstResponse.Content.ReadFromJsonAsync<CalculationResultDto>())!;
        Assert.Equal(175m, first.TotalCost);

        (await client.PutAsJsonAsync($"/api/company/products/{product.Id}", new
        {
            name = product.Name, aeciProductCode = "S100", pricePerKg = 50m, isActive = true
        })).EnsureSuccessStatusCode();

        var secondResponse = await client.PostAsJsonAsync($"/api/projects/{projectId}/calculations",
            new { delayWindowMilliseconds = 50 });
        Assert.Equal(HttpStatusCode.Created, secondResponse.StatusCode);
        var second = (await secondResponse.Content.ReadFromJsonAsync<CalculationResultDto>())!;
        Assert.Equal(250m, second.TotalCost);

        using var verifyScope = host.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(175m, (await verifyDb.CalculationResults.SingleAsync(r => r.Id == first.Id)).TotalCost);
    }
}
