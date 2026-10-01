using System.ComponentModel.DataAnnotations;

namespace BlastPro.Mvc.Models.ViewModels.Account;

public sealed class CreateCompanyViewModel
{
    [Required, StringLength(150), Display(Name = "Company name")] public string CompanyName { get; set; } = "";
    [Required, StringLength(80), Display(Name = "Registration number")] public string RegistrationNumber { get; set; } = "";
    [Required, EmailAddress, StringLength(254), Display(Name = "Company email")] public string ContactEmail { get; set; } = "";
    [Required, Phone, StringLength(30), Display(Name = "Company phone")] public string ContactPhone { get; set; } = "";
    [StringLength(300)] public string? Address { get; set; }
    [Required, StringLength(150), Display(Name = "Your full name")] public string FullName { get; set; } = "";
    [Required, EmailAddress, StringLength(254), Display(Name = "Your email")] public string Email { get; set; } = "";
    [Required, StringLength(100, MinimumLength = 8), DataType(DataType.Password)] public string Password { get; set; } = "";
    [Required, DataType(DataType.Password), Compare(nameof(Password)), Display(Name = "Confirm password")] public string ConfirmPassword { get; set; } = "";
}

public sealed class EmailTokenViewModel
{
    [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = "";
    [Required, StringLength(4096)] public string Token { get; set; } = "";
}
