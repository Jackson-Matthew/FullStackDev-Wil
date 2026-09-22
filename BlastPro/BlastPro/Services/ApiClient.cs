using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BlastPro.Mvc.Services.Interfaces;

namespace BlastPro.Mvc.Services;

public class ApiClient : IApiClient
{
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApiClient(HttpClient http, IHttpContextAccessor httpContextAccessor)
    {
        _http = http;
        _httpContextAccessor = httpContextAccessor;
    }

    private void SetCurrentUserToken()
    {
        if (_httpContextAccessor.HttpContext is { } context)
            SetBearerToken(context.User.FindFirst("jwt")?.Value);
    }

    public void SetBearerToken(string? token)
    {
        _http.DefaultRequestHeaders.Authorization = string.IsNullOrEmpty(token)
            ? null
            : new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<ApiResult<T>> GetAsync<T>(string path)
    {
        try
        {
            SetCurrentUserToken();
            var response = await _http.GetAsync(path);
            return await ReadResultAsync<T>(response);
        }
        catch (Exception ex)
        {
            return new ApiResult<T> { Success = false, Error = ex.Message };
        }
    }

    public async Task<ApiResult<T>> PostAsync<T>(string path, object payload)
    {
        try
        {
            SetCurrentUserToken();
            var response = await _http.PostAsJsonAsync(path, payload);
            return await ReadResultAsync<T>(response);
        }
        catch (Exception ex)
        {
            return new ApiResult<T> { Success = false, Error = ex.Message };
        }
    }

    public async Task<ApiResult<T>> PutAsync<T>(string path, object payload)
    {
        try
        {
            SetCurrentUserToken();
            var response = await _http.PutAsJsonAsync(path, payload);
            return await ReadResultAsync<T>(response);
        }
        catch (Exception ex)
        {
            return new ApiResult<T> { Success = false, Error = ex.Message };
        }
    }

    public async Task<ApiResult> DeleteAsync(string path)
    {
        try
        {
            SetCurrentUserToken();
            var response = await _http.DeleteAsync(path);
            if (response.IsSuccessStatusCode) return new ApiResult { Success = true };

            var body = await response.Content.ReadAsStringAsync();
            return new ApiResult { Success = false, Error = body };
        }
        catch (Exception ex)
        {
            return new ApiResult { Success = false, Error = ex.Message };
        }
    }

    public Task<ApiResult<LoginResultDto>> LoginAsync(string email, string password)
        => PostAsync<LoginResultDto>("api/auth/login", new { email, password });

    public async Task<ApiResult> ForgotPasswordAsync(string email)
    {
        var r = await PostAsync<object>("api/auth/forgot-password", new { email });
        return new ApiResult { Success = r.Success, Error = r.Error };
    }

    public async Task<ApiResult> ResetPasswordAsync(string email, string token, string newPassword)
    {
        var r = await PostAsync<object>(
            "api/auth/reset-password",
            new { email, token, newPassword });
        return new ApiResult { Success = r.Success, Error = r.Error };
    }

    private static async Task<ApiResult<T>> ReadResultAsync<T>(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.NoContent)
                return new ApiResult<T> { Success = true };

            var data = await response.Content.ReadFromJsonAsync<T>();
            return new ApiResult<T> { Success = true, Data = data };
        }

        var error = await response.Content.ReadAsStringAsync();
        return new ApiResult<T> { Success = false, Error = error };
    }
}
