using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BlastPro.Mvc.Services.Interfaces;

namespace BlastPro.Mvc.Services;

public class ApiClient(HttpClient http, IHttpContextAccessor contextAccessor) : IApiClient
{
    private const string Unavailable = "The service is temporarily unavailable. Please try again shortly.";

    public Task<ApiResult<T>> GetAsync<T>(string path) => SendAsync<T>(HttpMethod.Get, path);
    public Task<ApiResult<T>> PostAsync<T>(string path, object payload) => SendAsync<T>(HttpMethod.Post, path, payload);
    public Task<ApiResult<T>> PutAsync<T>(string path, object payload) => SendAsync<T>(HttpMethod.Put, path, payload);
    public async Task<ApiResult> DeleteAsync(string path) => await SendAsync<object>(HttpMethod.Delete, path);
    public Task<ApiResult<LoginResultDto>> LoginAsync(string email, string password)
        => SendAsync<LoginResultDto>(HttpMethod.Post, "api/auth/login", new { email, password }, false);
    public Task<ApiResult<PasswordResetDeliveryDto>> ForgotPasswordAsync(string email)
        => SendAsync<PasswordResetDeliveryDto>(HttpMethod.Post, "api/auth/forgot-password", new { email }, false);
    public async Task<ApiResult> ResetPasswordAsync(string email, string token, string newPassword)
        => await SendAsync<object>(HttpMethod.Post, "api/auth/reset-password", new { email, token, newPassword }, false);

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string path, object? payload = null,
        bool authenticated = true)
    {
        using var request = new HttpRequestMessage(method, path);
        if (payload is not null) request.Content = JsonContent.Create(payload);
        // Resolve the principal on each send; never store a user's token in a pooled handler.
        if (authenticated)
        {
            var token = contextAccessor.HttpContext?.User.FindFirst("jwt")?.Value;
            if (string.IsNullOrEmpty(token)) throw new ApiAuthenticationException();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        try
        {
            using var response = await http.SendAsync(request);
            if (authenticated && response.StatusCode == HttpStatusCode.Unauthorized)
                throw new ApiAuthenticationException();
            if (response.IsSuccessStatusCode)
            {
                var data = response.StatusCode == HttpStatusCode.NoContent
                    ? default : await response.Content.ReadFromJsonAsync<T>();
                return new ApiResult<T> { Success = true, Data = data, StatusCode = response.StatusCode };
            }
            var error = (int)response.StatusCode >= 500 ? Unavailable : "The request could not be completed.";
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                if (body.RootElement.TryGetProperty("errors", out var errors))
                {
                    var messages = errors.ValueKind == JsonValueKind.Array
                        ? errors.EnumerateArray().Select(x => x.GetString())
                        : errors.EnumerateObject().SelectMany(x => x.Value.EnumerateArray().Select(v => v.GetString()));
                    error = string.Join(" ", messages.Where(x => !string.IsNullOrWhiteSpace(x)));
                }
                else if (body.RootElement.TryGetProperty("message", out var message))
                    error = message.GetString() ?? error;
            }
            return new ApiResult<T> { Success = false, Error = error, StatusCode = response.StatusCode };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return new ApiResult<T> { Success = false, Error = Unavailable };
        }
    }
}

public sealed class ApiAuthenticationException : Exception;
