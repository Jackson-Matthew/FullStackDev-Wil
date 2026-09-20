namespace BlastPro.Mvc.Services.Interfaces;

public interface IApiClient
{
    void SetBearerToken(string? token);

    Task<ApiResult<T>> GetAsync<T>(string path);
    Task<ApiResult<T>> PostAsync<T>(string path, object payload);
    Task<ApiResult<T>> PutAsync<T>(string path, object payload);
    Task<ApiResult> DeleteAsync(string path);

    Task<ApiResult<LoginResultDto>> LoginAsync(string email, string password);
    Task<ApiResult> ForgotPasswordAsync(string email);
    Task<ApiResult> ResetPasswordAsync(string email, string token, string newPassword);
}

public class ApiResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
}

public class ApiResult<T> : ApiResult
{
    public T? Data { get; init; }
}

public class LoginResultDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int CompanyId { get; set; }
    public List<string> Roles { get; set; } = new();
}