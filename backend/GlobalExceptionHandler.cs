using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Csm.PixelGrove;

internal class Result
{
    private Result() { }
}

internal sealed partial class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> logger;
    private readonly IProblemDetailsService problemDetails;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IProblemDetailsService problemDetails)
    {
        this.logger = logger;
        this.problemDetails = problemDetails;
    }

    public ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        this.LogUnhandledExceptionOccurred(exception);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        return this.problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Title = "Internal Server Error",
                Detail = exception.Message,
            },
        });
    }

    [LoggerMessage(LogLevel.Error, "Unhandled exception occurred.")]
    partial void LogUnhandledExceptionOccurred(Exception exception);
}
