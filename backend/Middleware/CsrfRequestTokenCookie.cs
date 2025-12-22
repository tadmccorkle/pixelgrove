using System.Threading.Tasks;
using Csm.PixelGrove.Auth;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;

namespace Csm.PixelGrove.Middleware;

internal class CsrfRequestTokenCookie
{
    private readonly RequestDelegate next;
    private readonly IAntiforgery antiforgery;

    public CsrfRequestTokenCookie(RequestDelegate next, IAntiforgery antiforgery)
    {
        this.next = next;
        this.antiforgery = antiforgery;
    }

    public Task InvokeAsync(HttpContext context)
    {
        var tokens = this.antiforgery.GetAndStoreTokens(context);

        if (tokens.RequestToken is not null)
        {
            context.Response.Cookies.Append(AuthConfiguration.CsrfCookieName, tokens.RequestToken, new CookieOptions
            {
                HttpOnly = false,
                SameSite = SameSiteMode.Lax,
                Path = "/",
            });
        }

        return this.next(context);
    }
}
