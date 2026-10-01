using BlastPro.Api.Data;
using BlastPro.Api.Models.Dtos;
using BlastPro.Api.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlastPro.Api.Controllers;

[ApiController, Authorize, Route("api/profile")]
public sealed class ProfileController(ApplicationDbContext db, UserManager<ApplicationUser> users) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var user = await users.GetUserAsync(User);
        if (user is null) return Unauthorized();
        var companyName = await db.Companies.Where(c => c.Id == user.CompanyId).Select(c => c.Name).SingleAsync();
        return Ok(new ProfileDto
        {
            FullName = user.FullName, Email = user.Email!, PhoneNumber = user.PhoneNumber,
            NickName = user.NickName, Gender = user.Gender, Country = user.Country, TimeZoneId = user.TimeZoneId,
            CertificationId = user.CertificationId, CompanyName = companyName, Roles = await users.GetRolesAsync(user)
        });
    }

    [HttpPut]
    public async Task<IActionResult> Update(ProfileDetailsRequest request)
    {
        var user = await users.GetUserAsync(User);
        if (user is null) return Unauthorized();
        user.FullName = request.FullName.Trim();
        user.PhoneNumber = request.PhoneNumber?.Trim();
        user.NickName = request.NickName?.Trim();
        user.Gender = request.Gender?.Trim();
        user.Country = request.Country?.Trim();
        user.TimeZoneId = request.TimeZoneId?.Trim();
        user.UpdatedAtUtc = DateTime.UtcNow;
        var result = await users.UpdateAsync(user);
        return result.Succeeded ? NoContent() : BadRequest(new { errors = result.Errors.Select(e => e.Description) });
    }
}
