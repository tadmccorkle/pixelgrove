using System.Threading.Tasks;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Csm.PixelGrove.Middleware;

internal partial class CsrfEndpointFilter : IEndpointFilter
{
    private readonly ILogger logger;
    private readonly IAntiforgery antiforgery;

    public CsrfEndpointFilter(ILogger<CsrfEndpointFilter> logger, IAntiforgery antiforgery)
    {
        this.logger = logger;
        this.antiforgery = antiforgery;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.HttpContext.Request;

        if (request.Cookies.ContainsKey("auth") && !request.Headers.ContainsKey("Authorization"))
        {
            try
            {
                await this.antiforgery.ValidateRequestAsync(context.HttpContext);
            }
            catch (AntiforgeryValidationException ex)
            {
                this.LogInvalidAntiforgeryCookie(ex.Message);
                return Results.Unauthorized();
            }
        }

        return await next(context);
    }

    [LoggerMessage(LogLevel.Error, "Invalid antiforgery cookie: {exMessage}")]
    partial void LogInvalidAntiforgeryCookie(string exMessage);
}