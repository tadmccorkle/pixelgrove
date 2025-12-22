using System.Threading.Tasks;
using Csm.PixelGrove.Auth;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Csm.PixelGrove.Middleware;

internal partial class CsrfEndpointFilter : IEndpointFilter
{
    private readonly ILogger<CsrfEndpointFilter> logger;
    private readonly IAntiforgery antiforgery;

    public CsrfEndpointFilter(ILogger<CsrfEndpointFilter> logger, IAntiforgery antiforgery)
    {
        this.logger = logger;
        this.antiforgery = antiforgery;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.HttpContext.Request;

        var isRequestSafe = HttpMethods.IsGet(request.Method) ||
                            HttpMethods.IsHead(request.Method) ||
                            HttpMethods.IsOptions(request.Method) ||
                            HttpMethods.IsTrace(request.Method);
        var hasAuthCookie = request.Cookies.ContainsKey(AuthConfiguration.AuthCookieName);

        if (!isRequestSafe && hasAuthCookie)
        {
            try
            {
                await this.antiforgery.ValidateRequestAsync(context.HttpContext);
            }
            catch (AntiforgeryValidationException ex)
            {
                this.LogInvalidAntiforgeryToken(ex.Message, request.Path);
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }
        }

        return await next(context);
    }

    [LoggerMessage(LogLevel.Warning, "Invalid antiforgery token. Path: {Path}. Reason: {Message}")]
    partial void LogInvalidAntiforgeryToken(string message, string path);
}
