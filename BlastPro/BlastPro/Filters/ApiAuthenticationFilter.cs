using BlastPro.Mvc.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BlastPro.Mvc.Filters;

public sealed class ApiAuthenticationFilter : IAsyncExceptionFilter
{
    public async Task OnExceptionAsync(ExceptionContext context)
    {
        if (context.Exception is not ApiAuthenticationException) return;
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        // Never replay a POST or copy query strings that could contain reset tokens.
        var returnUrl = HttpMethods.IsGet(context.HttpContext.Request.Method)
            ? context.HttpContext.Request.Path.Value : null;
        context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl, expired = true });
        context.ExceptionHandled = true;
    }
}
