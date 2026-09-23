using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace BlastPro.Api.Services;

public static class PasswordResetTokens
{
    public static string Encode(string token) => WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
    public static string? Decode(string token)
    {
        try { return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token)); }
        catch (FormatException) { return null; }
    }
}
