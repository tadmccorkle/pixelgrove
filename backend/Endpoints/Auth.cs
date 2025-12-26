// Copyright (c) 2026 by Tad McCorkle
// Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Csm.PixelGrove.Auth;
using Csm.PixelGrove.Middleware;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Csm.PixelGrove.Endpoints;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
internal sealed partial class Auth : IEndpoint
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/auth/login", (string? provider, string? returnUrl) =>
        {
            provider ??= string.Empty;
            var scheme = provider.ToLowerInvariant().AsSpan().Trim() switch
            {
                "google" or _ => GoogleDefaults.AuthenticationScheme,
            };

            var redirectUri = "/";
            if (!string.IsNullOrEmpty(returnUrl) && IsRelativeUri(returnUrl) &&
                !returnUrl.StartsWith("//", StringComparison.Ordinal))
            {
                redirectUri = returnUrl.StartsWith('/') ? returnUrl : $"/{returnUrl}";
            }

            return Results.Challenge(new AuthenticationProperties { RedirectUri = redirectUri }, [scheme]);

            static bool IsRelativeUri(string url) => Uri.TryCreate(url, UriKind.Relative, out _);
        });

        endpoints.MapPost("/auth/logout", async (HttpContext context, ILogger<Auth> logger) =>
        {
            if (!context.User.TryGetAppUserId(out var userId))
                userId = Guid.Empty;

            await context.SignOutAsync(AuthConfiguration.CookiePolicyScheme);

            LogUserLoggedOut(logger, userId);

            context.Response.Redirect("/");
        }).AddEndpointFilter<IEndpointConventionBuilder, CsrfEndpointFilter>().RequireAuthorization();
    }

    [LoggerMessage(LogLevel.Information, "User {UserId} logged out.")]
    static partial void LogUserLoggedOut(ILogger<Auth> logger, Guid userId);
}
