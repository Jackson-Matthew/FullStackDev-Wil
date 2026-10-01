using BlastPro.Api.Data;
using BlastPro.Api.Extensions;
using BlastPro.Api.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlastPro.Api.Controllers;

[ApiController, Route("api/company"), Authorize(Roles = DatabaseSeeder.MainCompanyUserRole)]
public sealed class CompanyController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var company = await db.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == User.GetCompanyId());
        if (company is null) return NotFound();
        var blasters = await BlastersController.CompanyBlasters(db, company.Id).AsNoTracking()
            .OrderBy(u => u.FullName).Select(u => new BlasterDto(u.Id, u.FullName, u.Email!, u.PhoneNumber,
                u.CertificationId, u.IsActive, u.EmailConfirmed)).ToListAsync();
        return Ok(new CompanyDto(company.Id, company.Name, company.RegistrationNumber, company.ContactEmail,
            company.ContactPhone, company.Address, blasters.Count(u => u.IsActive), BlastersController.SeatLimit, blasters));
    }

    [HttpPut]
    public async Task<IActionResult> Update(CompanyContactRequest request)
    {
        var company = await db.Companies.FirstOrDefaultAsync(c => c.Id == User.GetCompanyId());
        if (company is null) return NotFound();
        company.ContactEmail = request.ContactEmail.Trim();
        company.ContactPhone = request.ContactPhone.Trim();
        company.Address = request.Address?.Trim();
        company.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }
}
