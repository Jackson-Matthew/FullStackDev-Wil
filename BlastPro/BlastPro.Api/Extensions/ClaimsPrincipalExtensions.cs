using System.Security.Claims;

namespace BlastPro.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string? GetUserId(this ClaimsPrincipal user)
        => user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public static int? GetCompanyId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst("companyId")?.Value;
        return int.TryParse(value, out var id) ? id : null;
    }

    public static string? GetEmail(this ClaimsPrincipal user)
        => user.FindFirst(ClaimTypes.Email)?.Value;
}