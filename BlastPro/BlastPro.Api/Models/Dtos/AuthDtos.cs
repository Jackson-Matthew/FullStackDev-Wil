namespace BlastPro.Api.Models.Dtos;

public record LoginRequest(string Email, string Password);

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

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(string Email, string Token, string NewPassword);