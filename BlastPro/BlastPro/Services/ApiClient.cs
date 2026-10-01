using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BlastPro.Mvc.Services.Interfaces;

namespace BlastPro.Mvc.Services;

public class ApiClient(HttpClient http, IHttpContextAccessor contextAccessor) : IApiClient
{
    private void SetCurrentUserToken()
    {
        if (contextAccessor.HttpContext is { } ctx)
            SetBearerToken(ctx.User.FindFirst("jwt")?.Value);
    }

    public void SetBearerToken(string? token)
    {
        http.DefaultRequestHeaders.Authorization = string.IsNullOrEmpty(token)
            ? null
            : new AuthenticationHeaderValue("Bearer", token);
    }

    public Task<ApiResult<T>> GetAsync<T>(string path) => SendAsync<T>(HttpMethod.Get, path);
    public Task<ApiResult<T>> PostAsync<T>(string path, object payload) => SendAsync<T>(HttpMethod.Post, path, payload);
    public Task<ApiResult<T>> PostAnonymousAsync<T>(string path, object payload,
        CancellationToken cancellationToken = default)
        => SendAsync<T>(HttpMethod.Post, path, payload, false, cancellationToken);
    public Task<ApiResult<T>> PutAsync<T>(string path, object payload) => SendAsync<T>(HttpMethod.Put, path, payload);
    public async Task<ApiResult> DeleteAsync(string path) => await SendAsync<object>(HttpMethod.Delete, path);
    public Task<ApiResult<LoginResultDto>> LoginAsync(string email, string password)
        => SendAsync<LoginResultDto>(HttpMethod.Post, "api/auth/login", new { email, password }, false);
    public Task<ApiResult<PasswordResetDeliveryDto>> ForgotPasswordAsync(string email)
        => SendAsync<PasswordResetDeliveryDto>(HttpMethod.Post, "api/auth/forgot-password", new { email }, false);
    public async Task<ApiResult> ResetPasswordAsync(string email, string token, string newPassword)
        => await SendAsync<object>(HttpMethod.Post, "api/auth/reset-password", new { email, token, newPassword }, false);

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string path, object? payload = null,
        bool authenticated = true, CancellationToken cancellationToken = default)
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
            using var response = await http.SendAsync(request, cancellationToken);
            if (authenticated && response.StatusCode == HttpStatusCode.Unauthorized)
                throw new ApiAuthenticationException();
            if (response.IsSuccessStatusCode)
            {
                var data = response.StatusCode == HttpStatusCode.NoContent
                    ? default : await response.Content.ReadFromJsonAsync<T>();
                return new ApiResult<T> { Success = true, Data = data, StatusCode = response.StatusCode };
            }
            var error = (int)response.StatusCode >= 500
                ? "The BlastPro server could not complete the request."
                : "The request could not be completed.";
            Dictionary<string, string[]>? validationErrors = null;
            if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict)
            {
                using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                if (body.RootElement.TryGetProperty("errors", out var errors))
                {
                    if (errors.ValueKind == JsonValueKind.Object)
                    {
                        validationErrors = errors.EnumerateObject().ToDictionary(
                            property => property.Name,
                            property => property.Value.EnumerateArray()
                                .Select(value => value.GetString() ?? string.Empty)
                                .Where(message => !string.IsNullOrWhiteSpace(message)).ToArray());
                        error = string.Join(" ", validationErrors.Values.SelectMany(messages => messages));
                    }
                    else if (errors.ValueKind == JsonValueKind.Array)
                    {
                        error = string.Join(" ", errors.EnumerateArray()
                            .Select(value => value.GetString())
                            .Where(message => !string.IsNullOrWhiteSpace(message)));
                    }
                }
                else if (body.RootElement.TryGetProperty("message", out var message))
                    error = message.GetString() ?? error;
            }
            return new ApiResult<T> { Success = false, Error = error,
                ValidationErrors = validationErrors, StatusCode = response.StatusCode };
        }
        catch (HttpRequestException)
        {
            return new ApiResult<T> { Success = false, Error = "The connection to the BlastPro server failed." };
        }
        catch (TaskCanceledException)
        {
            return new ApiResult<T> { Success = false, Error = "The BlastPro server took too long to respond." };
        }
        catch (JsonException)
        {
            return new ApiResult<T> { Success = false, Error = "The BlastPro server returned a response the web app could not read." };
        }
    }
}
public sealed class ApiAuthenticationException : Exception
{
    public ApiAuthenticationException(string? message = null) : base(message) { }
}
