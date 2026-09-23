using System.Net;
using BlastPro.Api.Services.Interfaces;
using Microsoft.AspNetCore.WebUtilities;

namespace BlastPro.Api.Services;

// Local-only mailbox. Messages are never served by the API or committed to Git.
public sealed class DevelopmentPasswordResetDelivery(
    IWebHostEnvironment environment, IConfiguration configuration) : IPasswordResetDelivery
{
    private string Mailbox => Path.Combine(environment.ContentRootPath, "App_Data", "PasswordReset");
    private string? FrontendUrl => configuration["PasswordReset:FrontendUrl"];
    private bool IsAvailable => environment.IsDevelopment()
        && configuration.GetValue<bool>("PasswordReset:DevelopmentMailboxEnabled")
        && Uri.TryCreate(FrontendUrl, UriKind.Absolute, out var url)
        && url.IsLoopback && url.Scheme == Uri.UriSchemeHttps;

    public async Task PrepareAsync()
    {
        if (!IsAvailable) throw new InvalidOperationException("Local password reset delivery is not configured.");
        Directory.CreateDirectory(Mailbox);
        // Check mailbox health for every request, including unknown email addresses.
        var path = Path.Combine(Mailbox, Guid.NewGuid().ToString("N") + ".tmp");
        await using var probe = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None,
            4096, FileOptions.DeleteOnClose | FileOptions.Asynchronous);
        await probe.WriteAsync(new byte[] { 0 });
    }

    public async Task SendAsync(string email, string encodedToken)
    {
        if (!IsAvailable) throw new InvalidOperationException("Local password reset delivery is not configured.");
        var link = QueryHelpers.AddQueryString(
            new Uri(new Uri(FrontendUrl!), "/Account/ResetPassword").AbsoluteUri,
            new Dictionary<string, string?> { ["email"] = email, ["token"] = encodedToken });
        var html = $"<!doctype html><html lang=\"en\"><meta charset=\"utf-8\"><title>BlastPro password reset</title>"
            + "<h1>Reset your BlastPro password</h1>"
            + $"<p>Local development message for {WebUtility.HtmlEncode(email)}. No email was sent.</p>"
            + $"<p><a href=\"{WebUtility.HtmlEncode(link)}\">Reset password</a></p>"
            + "<p>This link expires after one hour and can be used once. Delete this message when finished.</p></html>";
        var file = Path.Combine(Mailbox, $"{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}.html");
        await File.WriteAllTextAsync(file, html);
    }
}
