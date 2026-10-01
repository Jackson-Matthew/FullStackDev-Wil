using System.ComponentModel.DataAnnotations;

namespace BlastPro.Api.Models.Dtos;

public sealed class CreateCompanyRequest
{
    [Required, StringLength(150)] public string CompanyName { get; set; } = "";
    [Required, StringLength(80)] public string RegistrationNumber { get; set; } = "";
    [Required, EmailAddress, StringLength(254)] public string ContactEmail { get; set; } = "";
    [Required, Phone, StringLength(30)] public string ContactPhone { get; set; } = "";
    [StringLength(300)] public string? Address { get; set; }
    [Required, StringLength(150)] public string FullName { get; set; } = "";
    [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = "";
    [Required, StringLength(100, MinimumLength = 8)] public string Password { get; set; } = "";
}

public sealed class CompanyContactRequest
{
    [Required, EmailAddress, StringLength(254)] public string ContactEmail { get; set; } = "";
    [Required, Phone, StringLength(30)] public string ContactPhone { get; set; } = "";
    [StringLength(300)] public string? Address { get; set; }
}

public class BlasterDetailsRequest
{
    [Required, StringLength(150)] public string FullName { get; set; } = "";
    [Required, Phone, StringLength(30)] public string PhoneNumber { get; set; } = "";
    [Required, StringLength(80)] public string CertificationId { get; set; } = "";
}

public sealed class CreateBlasterRequest : BlasterDetailsRequest
{
    [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = "";
}

public record BlasterStatusRequest([Required] bool? IsActive);
public record AcceptInvitationRequest(
    [Required, EmailAddress, StringLength(254)] string Email,
    [Required, StringLength(4096)] string Token,
    [Required, StringLength(100, MinimumLength = 8)] string Password);

public record BlasterDto(string Id, string FullName, string Email, string? PhoneNumber,
    string? CertificationId, bool IsActive, bool EmailConfirmed);

public record CompanyDto(int Id, string Name, string? RegistrationNumber, string? ContactEmail,
    string? ContactPhone, string? Address, int ActiveBlasterCount, int BlasterLimit, List<BlasterDto> Blasters);
