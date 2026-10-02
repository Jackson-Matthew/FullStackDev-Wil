using BlastPro.Api.Data;
using BlastPro.Api.Extensions;
using BlastPro.Api.Models.Dtos;
using BlastPro.Api.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlastPro.Api.Controllers;

[ApiController, Route("api/company/products"), Authorize(Roles = DatabaseSeeder.MainCompanyUserRole)]
public sealed class ProductsController(ApplicationDbContext db) : ControllerBase
{
    private static readonly (string Name, decimal Price)[] SamplePrices =
    [
        ("Bulk emulsion", 35m),
        ("Heavy ANFO", 28m),
        ("Packaged emulsion", 55m)
    ];

    [HttpGet]
    public async Task<ActionResult<List<ProductDto>>> Get()
    {
        var companyId = User.GetCompanyId();
        if (companyId is null) return Unauthorized();
        return Ok(await db.ExplosiveProducts.AsNoTracking()
            .Where(p => p.CompanyId == companyId)
            .OrderBy(p => p.Name)
            .Select(p => new ProductDto(p.Id, p.Name, p.PricePerKg, p.CurrencyCode,
                p.IsActive, p.UpdatedAtUtc)).ToListAsync());
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(SaveProductRequest request)
    {
        var companyId = User.GetCompanyId();
        if (companyId is null) return Unauthorized();
        var name = request.Name.Trim();
        if (await NameExists(companyId.Value, name))
            return Conflict(new { message = "This company already has a product with that name." });
        var now = DateTime.UtcNow;
        var product = new ExplosiveProduct
        {
            CompanyId = companyId.Value, Name = name, PricePerKg = request.PricePerKg,
            CurrencyCode = "ZAR", IsActive = request.IsActive,
            CreatedAtUtc = now, UpdatedAtUtc = now
        };
        db.ExplosiveProducts.Add(product);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new ProductDto(product.Id, product.Name,
            product.PricePerKg, product.CurrencyCode, product.IsActive, product.UpdatedAtUtc));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, SaveProductRequest request)
    {
        var companyId = User.GetCompanyId();
        if (companyId is null) return Unauthorized();
        var product = await db.ExplosiveProducts.FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId);
        if (product is null) return NotFound();
        var name = request.Name.Trim();
        if (await NameExists(companyId.Value, name, id))
            return Conflict(new { message = "This company already has a product with that name." });
        product.Name = name;
        product.PricePerKg = request.PricePerKg;
        product.IsActive = request.IsActive;
        product.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("samples")]
    public async Task<ActionResult<List<ProductDto>>> AddSamples()
    {
        var companyId = User.GetCompanyId();
        if (companyId is null) return Unauthorized();
        var existing = await db.ExplosiveProducts.Where(p => p.CompanyId == companyId)
            .Select(p => p.Name.ToUpper()).ToListAsync();
        var now = DateTime.UtcNow;
        foreach (var (name, price) in SamplePrices)
        {
            if (existing.Contains(name.ToUpperInvariant())) continue;
            db.ExplosiveProducts.Add(new ExplosiveProduct
            {
                CompanyId = companyId.Value, Name = name, PricePerKg = price,
                CurrencyCode = "ZAR", IsActive = true,
                CreatedAtUtc = now, UpdatedAtUtc = now
            });
        }
        await db.SaveChangesAsync();
        return await Get();
    }

    private Task<bool> NameExists(int companyId, string name, int? exceptId = null) =>
        db.ExplosiveProducts.AnyAsync(p => p.CompanyId == companyId && p.Id != exceptId
            && p.Name.ToUpper() == name.ToUpper());
}
