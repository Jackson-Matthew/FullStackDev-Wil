using System.ComponentModel.DataAnnotations;

namespace BlastPro.Mvc.Models.ViewModels;

public sealed class ProfileViewModel
{
    [Required, StringLength(150), Display(Name = "Full name")] public string FullName { get; set; } = "";
    [Phone, StringLength(30), Display(Name = "Contact phone")] public string? PhoneNumber { get; set; }
    [StringLength(100), Display(Name = "Nickname")] public string? NickName { get; set; }
    [StringLength(50)] public string? Gender { get; set; }
    [StringLength(100)] public string? Country { get; set; }
    [StringLength(100), Display(Name = "Time zone")] public string? TimeZoneId { get; set; }
    public string Email { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public List<string> Roles { get; set; } = [];
    public string? CertificationId { get; set; }
}
