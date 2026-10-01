using System.ComponentModel.DataAnnotations;

namespace BlastPro.Mvc.Models.ViewModels;

public sealed class BlasterViewModel
{
    public string Id { get; set; } = "";
    [Required, StringLength(150), Display(Name = "Full name")] public string FullName { get; set; } = "";
    [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = "";
    [Required, Phone, StringLength(30), Display(Name = "Contact phone")] public string PhoneNumber { get; set; } = "";
    [Required, StringLength(80), Display(Name = "Certification ID")] public string CertificationId { get; set; } = "";
    public bool IsActive { get; set; }
    public bool EmailConfirmed { get; set; }
}
