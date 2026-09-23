using System.Net;
using System.Text.RegularExpressions;
using BlastPro.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace BlastPro.Tests.Unit;

public class PasswordResetDeliveryTests
{
    [Theory]
    [InlineData("Production", true, "https://localhost:7001")]
    [InlineData("Development", false, "https://localhost:7001")]
    [InlineData("Development", true, "https://external.example")]
    public async Task Mailbox_is_unavailable_outside_enabled_local_development(string environment, bool enabled, string url)
    {
        var service = new DevelopmentPasswordResetDelivery(new TestEnvironment { EnvironmentName = environment },
            Settings(enabled, url));
        await Assert.ThrowsAsync<InvalidOperationException>(service.PrepareAsync);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SendAsync("user@example.test", "test-token"));
    }

    [Fact]
    public async Task Local_message_contains_encoded_working_link_and_no_email_claim()
    {
        var root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "BlastProAuthTests", Guid.NewGuid().ToString("N")));
        var environment = new TestEnvironment { ContentRootPath = root };
        var service = new DevelopmentPasswordResetDelivery(environment, Settings(true, "https://localhost:7001"));
        var mailbox = Path.Combine(root, "App_Data", "PasswordReset");
        try
        {
            await service.PrepareAsync();
            Assert.Empty(Directory.GetFiles(mailbox));
            const string identityToken = "token+with/slashes=and padding";
            var encoded = PasswordResetTokens.Encode(identityToken);
            await service.SendAsync("blaster+reset@example.test", encoded);
            var path = Assert.Single(Directory.GetFiles(mailbox, "*.html"));
            var html = await File.ReadAllTextAsync(path);
            Assert.Contains("No email was sent", html);
            var link = WebUtility.HtmlDecode(Regex.Match(html, "href=\"([^\"]+)\"").Groups[1].Value);
            var uri = new Uri(link);
            Assert.Equal("https://localhost:7001/Account/ResetPassword", uri.GetLeftPart(UriPartial.Path));
            var query = QueryHelpers.ParseQuery(uri.Query);
            Assert.Equal("blaster+reset@example.test", query["email"]);
            Assert.Equal(identityToken, PasswordResetTokens.Decode(query["token"]!));
        }
        finally
        {
            // Delete only this test's unique temporary directory.
            var testRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "BlastProAuthTests")) + Path.DirectorySeparatorChar;
            if (root.StartsWith(testRoot, StringComparison.OrdinalIgnoreCase) && Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    private static IConfiguration Settings(bool enabled, string url) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PasswordReset:DevelopmentMailboxEnabled"] = enabled.ToString(),
            ["PasswordReset:FrontendUrl"] = url
        }).Build();

    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "BlastPro.Tests";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public string WebRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
