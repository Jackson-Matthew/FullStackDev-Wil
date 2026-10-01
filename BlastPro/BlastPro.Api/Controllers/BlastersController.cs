using BlastPro.Api.Data;
using BlastPro.Api.Extensions;
using BlastPro.Api.Models.Dtos;
using BlastPro.Api.Models.Entities;
using BlastPro.Api.Services;
using BlastPro.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlastPro.Api.Controllers;

[ApiController, Route("api/company/blasters"), Authorize(Roles = DatabaseSeeder.MainCompanyUserRole)]
public sealed class BlastersController(ApplicationDbContext db, UserManager<ApplicationUser> users,
    IAccountEmailDelivery delivery, ILogger<BlastersController> logger) : ControllerBase
{
    public const int SeatLimit = 5;
    public const string InvitationPurpose = "BlasterInvitation";

    internal static IQueryable<ApplicationUser> CompanyBlasters(ApplicationDbContext db, int companyId)
        => db.Users.Where(u => u.CompanyId == companyId && db.UserRoles.Any(ur => ur.UserId == u.Id
            && db.Roles.Any(r => r.Id == ur.RoleId && r.Name == DatabaseSeeder.BlasterRole)));

    private IQueryable<ApplicationUser> Blasters => CompanyBlasters(db, User.GetCompanyId() ?? 0);
    private static BlasterDto ToDto(ApplicationUser user) => new(user.Id, user.FullName, user.Email!,
        user.PhoneNumber, user.CertificationId, user.IsActive, user.EmailConfirmed);

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var user = await Blasters.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        return user is null ? NotFound() : Ok(ToDto(user));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateBlasterRequest request)
    {
        var companyId = User.GetCompanyId();
        if (companyId is null) return Unauthorized();
        try
        {
            await delivery.PrepareAsync();
            await using var transaction = await AccountTransactions.BeginAsync(db, companyId);
            if (await Blasters.CountAsync(u => u.IsActive) >= SeatLimit) return SeatFull();
            if (await users.FindByEmailAsync(request.Email.Trim()) is not null)
                return Conflict(new { message = "This email address is already registered." });
            if (await CertificationExists(request.CertificationId)) return CertificationConflict();
            var user = new ApplicationUser
            {
                CompanyId = companyId.Value, FullName = request.FullName.Trim(), Email = request.Email.Trim(),
                UserName = request.Email.Trim(), PhoneNumber = request.PhoneNumber.Trim(),
                CertificationId = request.CertificationId.Trim(), IsActive = true, EmailConfirmed = false,
                CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow
            };
            var result = await users.CreateAsync(user);
            if (!result.Succeeded) return IdentityErrors(result);
            result = await users.AddToRoleAsync(user, DatabaseSeeder.BlasterRole);
            if (!result.Succeeded) return IdentityErrors(result);
            await SendInvitation(user);
            if (transaction is not null) await transaction.CommitAsync();
            return StatusCode(201, ToDto(user));
        }
        catch (DbUpdateException) { return Conflict(new { message = "Email and company certification ID must be unique." }); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            logger.LogWarning("Blaster invitation could not be completed.");
            return StatusCode(503, new { message = "The Blaster invitation could not be sent." });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, BlasterDetailsRequest request)
    {
        var user = await Blasters.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();
        if (await CertificationExists(request.CertificationId, id)) return CertificationConflict();
        user.FullName = request.FullName.Trim();
        user.PhoneNumber = request.PhoneNumber.Trim();
        user.CertificationId = request.CertificationId.Trim();
        user.UpdatedAtUtc = DateTime.UtcNow;
        try
        {
            var result = await users.UpdateAsync(user);
            return result.Succeeded ? NoContent() : IdentityErrors(result);
        }
        catch (DbUpdateException) { return CertificationConflict(); }
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> SetStatus(string id, BlasterStatusRequest request)
    {
        var isActive = request.IsActive!.Value;
        await using var transaction = await AccountTransactions.BeginAsync(db, User.GetCompanyId());
        var user = await Blasters.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();
        if (isActive && !user.IsActive && await Blasters.CountAsync(u => u.IsActive) >= SeatLimit)
            return SeatFull();
        if (isActive != user.IsActive)
        {
            user.IsActive = isActive;
            user.UpdatedAtUtc = DateTime.UtcNow;
            // Deactivation permanently revokes existing sessions, even if the user is later reactivated.
            var result = await users.UpdateSecurityStampAsync(user);
            if (!result.Succeeded) return IdentityErrors(result);
        }
        if (transaction is not null) await transaction.CommitAsync();
        return NoContent();
    }

    [HttpPost("{id}/invitation")]
    public async Task<IActionResult> ResendInvitation(string id)
    {
        try
        {
            await delivery.PrepareAsync();
            await using var transaction = await AccountTransactions.BeginAsync(db, User.GetCompanyId());
            var user = await Blasters.FirstOrDefaultAsync(u => u.Id == id);
            if (user is null) return NotFound();
            if (!user.IsActive || user.EmailConfirmed || await users.HasPasswordAsync(user))
                return BadRequest(new { message = "Only active Blasters awaiting setup can be invited." });
            var result = await users.UpdateSecurityStampAsync(user);
            if (!result.Succeeded) return IdentityErrors(result);
            await SendInvitation(user);
            if (transaction is not null) await transaction.CommitAsync();
            return Ok(new { message = "A new invitation has been requested.", isDevelopmentDelivery = true });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            logger.LogWarning("Blaster invitation delivery is unavailable.");
            return StatusCode(503, new { message = "The Blaster invitation could not be sent." });
        }
    }

    private Task<bool> CertificationExists(string certification, string? exceptId = null)
        => db.Users.AnyAsync(u => u.CompanyId == User.GetCompanyId() && u.Id != exceptId
            && u.CertificationId == certification.Trim());
    private async Task SendInvitation(ApplicationUser user)
        => await delivery.SendInvitationAsync(user.Email!, PasswordResetTokens.Encode(
            await users.GenerateUserTokenAsync(user, TokenOptions.DefaultProvider, InvitationPurpose)));
    private IActionResult SeatFull() => Conflict(new { message = "This company already has 5 active Blasters. Deactivate a Blaster to free a seat." });
    private IActionResult CertificationConflict() => Conflict(new { message = "This certification ID is already in use in your company." });
    private IActionResult IdentityErrors(IdentityResult result) => BadRequest(new { errors = result.Errors.Select(e => e.Description) });
}
