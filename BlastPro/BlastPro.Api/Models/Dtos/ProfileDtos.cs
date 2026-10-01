using System.ComponentModel.DataAnnotations;

namespace BlastPro.Api.Models.Dtos;

public class ProfileDetailsRequest
{
    [Required, StringLength(150)] public string FullName { get; set; } = "";
    [Phone, StringLength(30)] public string? PhoneNumber { get; set; }
    [StringLength(100)] public string? NickName { get; set; }
    [StringLength(50)] public string? Gender { get; set; }
    [StringLength(100)] public string? Country { get; set; }
    [StringLength(100)] public string? TimeZoneId { get; set; }
}

public sealed class ProfileDto : ProfileDetailsRequest
{
    public string Email { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public IList<string> Roles { get; set; } = [];
    public string? CertificationId { get; set; }
}
