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

public sealed class CreateBlasterViewModel
{
    [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = "";
    [Required, StringLength(100, MinimumLength = 8), DataType(DataType.Password)]
    public string Password { get; set; } = "";
    [Required, Compare(nameof(Password)), DataType(DataType.Password), Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = "";
}

public sealed class BlasterPasswordViewModel
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    [Required, StringLength(100, MinimumLength = 8), DataType(DataType.Password), Display(Name = "New password")]
    public string Password { get; set; } = "";
    [Required, Compare(nameof(Password)), DataType(DataType.Password), Display(Name = "Confirm new password")]
    public string ConfirmPassword { get; set; } = "";
}
