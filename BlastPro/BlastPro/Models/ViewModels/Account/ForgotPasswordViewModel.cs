using System.ComponentModel.DataAnnotations;

namespace BlastPro.Mvc.Models.ViewModels.Account;

public class ForgotPasswordViewModel
{
    [Required, EmailAddress, Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;
}