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
        var blaster = await CompanyTestSetup.InviteAsync(companyA);
        using var blasterClient = await CompanyTestSetup.AcceptAsync(host, blaster);

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
        Assert.Equal(3, (await client.GetFromJsonAsync<List<ProductDto>>("/api/company/products"))!.Count);
    }
}
