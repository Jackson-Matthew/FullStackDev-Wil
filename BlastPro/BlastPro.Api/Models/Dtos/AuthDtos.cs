using System.ComponentModel.DataAnnotations;

namespace BlastPro.Api.Models.Dtos;

public record LoginRequest(
    [Required, EmailAddress, StringLength(254)] string Email,
    [Required] string Password);

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int CompanyId { get; set; }
    public IList<string> Roles { get; set; } = new List<string>();
}

public record ForgotPasswordRequest([Required, EmailAddress, StringLength(254)] string Email);

public record ResetPasswordRequest(
    [Required, EmailAddress, StringLength(254)] string Email,
    [Required, StringLength(4096)] string Token,
    [Required, StringLength(100, MinimumLength = 8)] string NewPassword);
