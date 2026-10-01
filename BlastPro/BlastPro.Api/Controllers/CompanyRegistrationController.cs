using BlastPro.Api.Data;
using BlastPro.Api.Models.Dtos;
using BlastPro.Api.Models.Entities;
using BlastPro.Api.Services;
using BlastPro.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlastPro.Api.Controllers;

[ApiController, AllowAnonymous, Route("api/companies")]
public sealed class CompanyRegistrationController(ApplicationDbContext db, UserManager<ApplicationUser> users,
    IAccountEmailDelivery delivery, ILogger<CompanyRegistrationController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateCompanyRequest request)
    {
        var registration = request.RegistrationNumber.Trim().ToUpperInvariant();
        if (await db.Companies.AnyAsync(c => c.RegistrationNumber == registration))
            return Conflict(new { message = "This company registration number is already registered." });
        if (await users.FindByEmailAsync(request.Email.Trim()) is not null)
            return Conflict(new { message = "This email address is already registered." });

        try
        {
            await delivery.PrepareAsync();
            await using var transaction = await AccountTransactions.BeginAsync(db);
            var company = new Company
            {
                Name = request.CompanyName.Trim(), RegistrationNumber = registration,
                ContactEmail = request.ContactEmail.Trim(), ContactPhone = request.ContactPhone.Trim(),
                Address = request.Address?.Trim(), CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow
            };
            var user = new ApplicationUser
            {
                Company = company, UserName = request.Email.Trim(), Email = request.Email.Trim(),
                FullName = request.FullName.Trim(), EmailConfirmed = false,
                CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow
            };
            // Identity saves the company navigation and user together, then assigns the server role.
            var result = await users.CreateAsync(user, request.Password);
            if (!result.Succeeded) return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
            result = await users.AddToRoleAsync(user, DatabaseSeeder.MainCompanyUserRole);
            if (!result.Succeeded) return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
            await delivery.SendConfirmationAsync(user.Email!,
                PasswordResetTokens.Encode(await users.GenerateEmailConfirmationTokenAsync(user)));
            if (transaction is not null) await transaction.CommitAsync();
            return StatusCode(201, new { message = "Company created. Confirm your email before signing in.", isDevelopmentDelivery = true });
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "Company registration number and user email must be unique. Please check your details." });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            logger.LogWarning("Company setup could not be completed.");
            return StatusCode(503, new { message = "Company setup is temporarily unavailable. Please try again later." });
        }
    }
}
