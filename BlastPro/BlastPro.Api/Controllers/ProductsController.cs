using BlastPro.Api.Data;
using BlastPro.Api.Extensions;
using BlastPro.Api.Models.Dtos;
using BlastPro.Api.Models.Entities;
using BlastPro.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlastPro.Api.Controllers;

[ApiController, Route("api/company/products"), Authorize(Roles = DatabaseSeeder.MainCompanyUserRole)]
public sealed class ProductsController(ApplicationDbContext db) : ControllerBase
{
    private static readonly (string Code, decimal Price)[] SamplePrices =
    [
        ("S100", 35m),
        ("S300", 28m),
        ("PG-ECO", 55m)
    ];

    [HttpGet("catalog")]
    public ActionResult<List<ProductCatalogDto>> Catalog() => Ok(AeciSurfaceProductCatalog.Products
        .Select(p => new ProductCatalogDto(p.Code, p.Name)).ToList());

    [HttpGet]
    public async Task<ActionResult<List<ProductDto>>> Get()
    {
        var companyId = User.GetCompanyId();
        if (companyId is null) return Unauthorized();
        return Ok(await db.ExplosiveProducts.AsNoTracking()
            .Where(p => p.CompanyId == companyId)
            .OrderBy(p => p.Name)
            .Select(p => new ProductDto(p.Id, p.Name, p.AeciProductCode, p.PricePerKg, p.CurrencyCode,
                p.IsActive, p.UpdatedAtUtc)).ToListAsync());
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(SaveProductRequest request)
    {
        var companyId = User.GetCompanyId();
        if (companyId is null) return Unauthorized();
        var name = request.Name.Trim();
        var code = string.IsNullOrWhiteSpace(request.AeciProductCode) ? null : request.AeciProductCode.Trim();
        if (code is not null && AeciSurfaceProductCatalog.Find(code) is null)
            return BadRequest(new { message = "Select a valid AECI catalogue product." });
        if (await NameExists(companyId.Value, name))
            return Conflict(new { message = "This company already has a product with that name." });
        if (code is not null && await CodeExists(companyId.Value, code))
            return Conflict(new { message = "This AECI product already has a company price." });
        var now = DateTime.UtcNow;
        var product = new ExplosiveProduct
        {
            CompanyId = companyId.Value, Name = name, AeciProductCode = code, PricePerKg = request.PricePerKg,
            CurrencyCode = "ZAR", IsActive = request.IsActive,
            CreatedAtUtc = now, UpdatedAtUtc = now
        };
        db.ExplosiveProducts.Add(product);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException) { return Conflict(new { message = "This company product or AECI price already exists." }); }
        return CreatedAtAction(nameof(Get), new ProductDto(product.Id, product.Name,
            product.AeciProductCode, product.PricePerKg, product.CurrencyCode, product.IsActive, product.UpdatedAtUtc));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, SaveProductRequest request)
    {
        var companyId = User.GetCompanyId();
        if (companyId is null) return Unauthorized();
        var product = await db.ExplosiveProducts.FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId);
        if (product is null) return NotFound();
        var name = request.Name.Trim();
        var code = string.IsNullOrWhiteSpace(request.AeciProductCode) ? null : request.AeciProductCode.Trim();
        if (code is not null && AeciSurfaceProductCatalog.Find(code) is null)
            return BadRequest(new { message = "Select a valid AECI catalogue product." });
        if (await NameExists(companyId.Value, name, id))
            return Conflict(new { message = "This company already has a product with that name." });
        if (code is not null && await CodeExists(companyId.Value, code, id))
            return Conflict(new { message = "This AECI product already has a company price." });
        product.Name = name;
        product.AeciProductCode = code;
        product.PricePerKg = request.PricePerKg;
        product.IsActive = request.IsActive;
        product.UpdatedAtUtc = DateTime.UtcNow;
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException) { return Conflict(new { message = "This company product or AECI price already exists." }); }
        return NoContent();
    }

    [HttpPost("samples")]
    public async Task<ActionResult<List<ProductDto>>> AddSamples()
    {
        var companyId = User.GetCompanyId();
        if (companyId is null) return Unauthorized();
        var existing = await db.ExplosiveProducts.Where(p => p.CompanyId == companyId).ToListAsync();
        var now = DateTime.UtcNow;
        foreach (var (code, price) in SamplePrices)
        {
            if (existing.Any(p => p.AeciProductCode == code)) continue;
            var name = AeciSurfaceProductCatalog.Find(code)!.Name;
            var named = existing.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (named is not null)
            {
                named.AeciProductCode = code;
                named.UpdatedAtUtc = now;
                continue;
            }
            db.ExplosiveProducts.Add(new ExplosiveProduct
            {
                CompanyId = companyId.Value, Name = name, AeciProductCode = code, PricePerKg = price,
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

    private Task<bool> CodeExists(int companyId, string code, int? exceptId = null) =>
        db.ExplosiveProducts.AnyAsync(p => p.CompanyId == companyId && p.Id != exceptId
            && p.AeciProductCode == code);
}
