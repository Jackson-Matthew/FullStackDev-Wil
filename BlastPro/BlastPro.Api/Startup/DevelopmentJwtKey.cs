using System.Security.Cryptography;

namespace BlastPro.Api.Startup;

internal static class DevelopmentJwtKey
{
    public static void EnsureConfigured(WebApplicationBuilder builder)
    {
        if (!builder.Environment.IsDevelopment() ||
            !string.IsNullOrWhiteSpace(builder.Configuration["Jwt:Key"]))
            return;

        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BlastPro", "Development");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "jwt.key");

        if (!File.Exists(path))
        {
            var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            try
            {
                using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                using var writer = new StreamWriter(file);
                writer.Write(key);
            }
            catch (IOException) when (File.Exists(path))
            {
                // Another local API process created the key at the same time.
            }
        }

        builder.Configuration["Jwt:Key"] = File.ReadAllText(path).Trim();
    }
}
