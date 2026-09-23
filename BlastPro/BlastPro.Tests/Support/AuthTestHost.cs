using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Text;
using BlastPro.Api.Controllers;
using BlastPro.Api.Data;
using BlastPro.Api.Models.Dtos;
using BlastPro.Api.Models.Entities;
using BlastPro.Api.Services.Interfaces;
using BlastPro.Mvc.Services;
using BlastPro.Mvc.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace BlastPro.Tests.Support;

public sealed class TestMailbox : IPasswordResetDelivery
{
    public ConcurrentDictionary<string, string> Messages { get; } = new();
    public bool Available { get; set; } = true;
    public Task PrepareAsync() => Available ? Task.CompletedTask
        : throw new IOException("Test delivery failure");
    public Task SendAsync(string email, string encodedToken)
    {
        Messages[email] = encodedToken;
        return Task.CompletedTask;
    }
}

public sealed class ApiTestHost(TimeSpan? resetLifetime = null, int tokenMinutes = 10)
    : WebApplicationFactory<AuthController>
{
    public const string Password = "TestPassword123!";
    public TestMailbox Mailbox { get; } = new();
    private readonly string _database = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "OnlyForAutomatedTests0123456789abcdefghijklmnopqrstuvwxyz",
            ["Jwt:Issuer"] = "BlastPro.Tests", ["Jwt:Audience"] = "BlastPro.Tests",
            ["Jwt:ExpiryMinutes"] = tokenMinutes.ToString()
        }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(_database));
            services.RemoveAll<IPasswordResetDelivery>();
            services.AddSingleton<IPasswordResetDelivery>(Mailbox);
            // Minimal hosting reads startup JWT settings before the factory's final config overrides.
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters.ValidIssuer = "BlastPro.Tests";
                options.TokenValidationParameters.ValidAudience = "BlastPro.Tests";
                options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes("OnlyForAutomatedTests0123456789abcdefghijklmnopqrstuvwxyz"));
            });
            if (resetLifetime.HasValue)
                services.PostConfigure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = resetLifetime.Value);
        });
    }

    public HttpClient Client() => CreateClient(new WebApplicationFactoryClientOptions
        { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

    public async Task<ApplicationUser> AddUserAsync(bool active = true, string? email = null)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync();
        var company = new Company { Name = Guid.NewGuid().ToString() };
        db.Companies.Add(company);
        await db.SaveChangesAsync();
        var user = new ApplicationUser
        {
            Email = email ?? $"blaster+{Guid.NewGuid():N}@example.test", FullName = "Test Blaster",
            CompanyId = company.Id, IsActive = active, EmailConfirmed = true
        };
        user.UserName = user.Email;
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var result = await manager.CreateAsync(user, Password);
        Assert.True(result.Succeeded, string.Join(" ", result.Errors.Select(x => x.Description)));
        Assert.True((await manager.AddToRoleAsync(user, "Blaster")).Succeeded);
        return user;
    }

    public async Task ChangeUserAsync(string id, Func<UserManager<ApplicationUser>, ApplicationUser, Task> update)
    {
        using var scope = Services.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await update(manager, (await manager.FindByIdAsync(id))!);
    }

    public async Task<LoginResponse> LoginAsync(HttpClient client, string email, string password = Password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
    }

    public async Task<string> RequestResetAsync(HttpClient client, string email)
    {
        (await client.PostAsJsonAsync("/api/auth/forgot-password", new { email })).EnsureSuccessStatusCode();
        return Mailbox.Messages[email];
    }
}

public sealed class MvcTestHost(ApiTestHost api, bool offline = false)
    : WebApplicationFactory<BlastPro.Mvc.Controllers.AccountController>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.AddHttpClient<IApiClient, ApiClient>()
                .ConfigurePrimaryHttpMessageHandler(() => offline ? new OfflineHandler() : api.Server.CreateHandler());
        });
    }

    public HttpClient Browser() => CreateClient(new WebApplicationFactoryClientOptions
        { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

    private sealed class OfflineHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new HttpRequestException("Private network details must not reach the page");
    }
}

public static class BrowserForms
{
    public static async Task<HttpResponseMessage> SubmitAsync(HttpClient browser, string getPath, string postPath,
        Dictionary<string, string> fields)
    {
        var page = await browser.GetAsync(getPath);
        page.EnsureSuccessStatusCode();
        var html = await page.Content.ReadAsStringAsync();
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        Assert.NotEmpty(token);
        fields["__RequestVerificationToken"] = WebUtility.HtmlDecode(token);
        return await browser.PostAsync(postPath, new FormUrlEncodedContent(fields));
    }
}
