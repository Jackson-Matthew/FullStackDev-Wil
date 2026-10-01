using System.Net;
using BlastPro.Api.Services.Interfaces;
using Microsoft.AspNetCore.WebUtilities;

namespace BlastPro.Api.Services;

public sealed class DevelopmentAccountEmailDelivery(
    IWebHostEnvironment environment, IConfiguration configuration) : IAccountEmailDelivery
{
    private string Mailbox => Path.Combine(environment.ContentRootPath, "App_Data", "AccountMail");
    private string? FrontendUrl => configuration["PasswordReset:FrontendUrl"];
    private bool IsAvailable => environment.IsDevelopment()
        && configuration.GetValue<bool>("PasswordReset:DevelopmentMailboxEnabled")
        && Uri.TryCreate(FrontendUrl, UriKind.Absolute, out var url)
        && url.IsLoopback && url.Scheme == Uri.UriSchemeHttps;

    public async Task PrepareAsync()
    {
        if (!IsAvailable) throw new InvalidOperationException("Local account email delivery is not configured.");
        Directory.CreateDirectory(Mailbox);
        var path = Path.Combine(Mailbox, Guid.NewGuid().ToString("N") + ".tmp");
        await using var probe = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None,
            4096, FileOptions.DeleteOnClose | FileOptions.Asynchronous);
        await probe.WriteAsync(new byte[] { 0 });
    }

    public Task SendConfirmationAsync(string email, string encodedToken)
        => SendAsync(email, encodedToken, "ConfirmEmail", "Confirm your BlastPro email");

    public Task SendInvitationAsync(string email, string encodedToken)
        => SendAsync(email, encodedToken, "AcceptInvitation", "Set your BlastPro password");

    private async Task SendAsync(string email, string encodedToken, string action, string title)
    {
        if (!IsAvailable) throw new InvalidOperationException("Local account email delivery is not configured.");
        var link = QueryHelpers.AddQueryString(new Uri(new Uri(FrontendUrl!), $"/Account/{action}").AbsoluteUri,
            new Dictionary<string, string?> { ["email"] = email, ["token"] = encodedToken });
        var html = "<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\">"
            + $"<title>{title}</title></head><body><h1>{title}</h1>"
            + $"<p>Local development message for {WebUtility.HtmlEncode(email)}. No email was sent.</p>"
            + $"<p><a href=\"{WebUtility.HtmlEncode(link)}\">{title}</a></p>"
            + "<p>This link expires after one hour. If it expires, request a new confirmation link or ask your company administrator for a new invitation.</p>"
            + "<p>If you did not expect this message, ignore it. Delete this message when finished.</p></body></html>";
        await File.WriteAllTextAsync(Path.Combine(Mailbox, $"{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}.html"), html);
    }
}
