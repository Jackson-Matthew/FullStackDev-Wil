using System.Net.Http.Headers;
using System.Net.Http.Json;
using BlastPro.Api.Models.Dtos;

namespace BlastPro.Tests.Support;

public static class CompanyTestSetup
{
    public static CreateCompanyRequest Registration() => new()
    {
        CompanyName = "Test Mining", RegistrationNumber = $"REG-{Guid.NewGuid():N}",
        ContactEmail = "office@example.test", ContactPhone = "+27 11 555 1234", Address = "Johannesburg",
        FullName = "Company Administrator", Email = $"main-{Guid.NewGuid():N}@example.test", Password = ApiTestHost.Password
    };

    public static async Task<CreateCompanyRequest> CreateMainAsync(ApiTestHost host, HttpClient client)
    {
        await host.InitializeDatabaseAsync();
        var registration = Registration();
        (await client.PostAsJsonAsync("/api/companies", registration)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/auth/confirm-email", new
            { registration.Email, token = host.AccountMailbox.Confirmations[registration.Email] })).EnsureSuccessStatusCode();
        var login = await host.LoginAsync(client, registration.Email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
        return registration;
    }

    public static object BlasterRequest(string? email = null, string? certification = null) => new
    {
        fullName = "Test Blaster", email = email ?? $"blaster-{Guid.NewGuid():N}@example.test",
        phoneNumber = "+27 82 555 1234", certificationId = certification ?? $"CERT-{Guid.NewGuid():N}",
        // Hostile extra fields must never influence account creation.
        companyId = 9999, role = "MainCompanyUser", password = "AdministratorKnows123!", isActive = false
    };

    public static async Task<BlasterDto> InviteAsync(HttpClient main)
    {
        var response = await main.PostAsJsonAsync("/api/company/blasters", BlasterRequest());
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BlasterDto>())!;
    }

    public static async Task<HttpClient> AcceptAsync(ApiTestHost host, BlasterDto blaster)
    {
        var client = host.Client();
        (await client.PostAsJsonAsync("/api/auth/accept-invitation", new
            { blaster.Email, token = host.AccountMailbox.Invitations[blaster.Email], password = ApiTestHost.Password })).EnsureSuccessStatusCode();
        var login = await host.LoginAsync(client, blaster.Email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
        return client;
    }
}
