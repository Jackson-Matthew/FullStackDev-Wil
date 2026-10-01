using System.ComponentModel.DataAnnotations;

namespace BlastPro.Mvc.Models.ViewModels.Account;

public sealed class CreateCompanyViewModel
{
    [Required(ErrorMessage = "Enter the company name."), StringLength(150), Display(Name = "Company name")] public string CompanyName { get; set; } = "";
    [Required(ErrorMessage = "Enter the registration number."), StringLength(80), Display(Name = "Registration number")] public string RegistrationNumber { get; set; } = "";
    [Required(ErrorMessage = "Enter the company email."), EmailAddress, StringLength(254), Display(Name = "Company email")] public string ContactEmail { get; set; } = "";
    [Required(ErrorMessage = "Enter the company phone number."), Phone, StringLength(30), Display(Name = "Company phone")] public string ContactPhone { get; set; } = "";
    [StringLength(300)] public string? Address { get; set; }
    [Required(ErrorMessage = "Enter your full name."), StringLength(150), Display(Name = "Your full name")] public string FullName { get; set; } = "";
    [Required(ErrorMessage = "Enter your email."), EmailAddress, StringLength(254), Display(Name = "Your email")] public string Email { get; set; } = "";
    [Required(ErrorMessage = "Enter a password."), StringLength(100, MinimumLength = 8), DataType(DataType.Password)] public string Password { get; set; } = "";
    [Required(ErrorMessage = "Confirm your password."), DataType(DataType.Password), Compare(nameof(Password)), Display(Name = "Confirm password")] public string ConfirmPassword { get; set; } = "";
}
