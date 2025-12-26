// Copyright (c) 2026 by Tad McCorkle
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Csm.PixelGrove.Auth;

internal static class AuthConfiguration
{
    public const string DefaultPolicyScheme = "MultiAuth";
    public const string CookiePolicyScheme = "Cookie";
    public const string BearerPolicyScheme = "Bearer";
    public const string ApiKeyPolicyScheme = "ApiKey";

    public const string WebOnlyPolicy = "WebOnly";
    public const string ApiOnlyPolicy = "ApiOnly";

    public const string AuthCookieName = "auth";

    public const string CsrfCookieName = "XSRF-TOKEN";
    public const string CsrfHeaderName = "X-XSRF-TOKEN";

    public static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var googleAuthConfig = GetGoogleOAuthConfig(configuration);

        services.AddScoped<GoogleOAuthEvents>();

        services.AddAuthentication(options =>
            {
                options.DefaultScheme = DefaultPolicyScheme;
                options.DefaultChallengeScheme = DefaultPolicyScheme;
            })
            .AddCookie(CookiePolicyScheme, options =>
            {
                options.Cookie.Name = AuthCookieName;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.Path = "/";
                options.ExpireTimeSpan = TimeSpan.FromDays(14);
                options.SlidingExpiration = true;
                options.LogoutPath = "/auth/logout";
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
            })
            // TODO(tad): add jwt bearer token auth
            // TODO(tad): add api-key auth
            .AddPolicyScheme(DefaultPolicyScheme, "Cookie or Header", options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    if (context.Request.Headers.TryGetValue("Authorization", out var authHeader) &&
                        authHeader.Count > 0)
                    {
                        var authType = authHeader.ToString();
                        if (authType.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                        {
                            // TODO(tad): return BearerScheme;
                        }

                        if (authType.StartsWith("ApiKey ", StringComparison.OrdinalIgnoreCase))
                        {
                            // TODO(tad): return ApiKeyScheme;
                        }
                    }

                    return CookiePolicyScheme;
                };
            })
            .AddGoogle(options =>
            {
                options.ClientId = googleAuthConfig.ClientId;
                options.ClientSecret = googleAuthConfig.ClientSecret;
                options.CallbackPath = googleAuthConfig.CallbackPath;
                options.EventsType = typeof(GoogleOAuthEvents);
                options.Events.OnRemoteFailure = context =>
                {
                    context.Response.Redirect("/login?error=auth");
                    context.HandleResponse();
                    return Task.CompletedTask;
                };
            });

        // NOTE(tad): These policies are for future use.
        // WebOnlyPolicy: cookie-based auth for browser clients
        // ApiOnlyPolicy: header-based auth (Bearer/ApiKey) for API clients
        services.AddAuthorizationBuilder()
            .AddPolicy(WebOnlyPolicy, policy
                => policy.RequireAuthenticatedUser().AddAuthenticationSchemes(CookiePolicyScheme))
            .AddPolicy(ApiOnlyPolicy, policy
                => policy.RequireAuthenticatedUser().AddAuthenticationSchemes(BearerPolicyScheme, ApiKeyPolicyScheme));

        services.AddAntiforgery(options => options.HeaderName = CsrfHeaderName);

        return services;
    }

    private static GoogleOAuthConfig GetGoogleOAuthConfig(IConfiguration configuration)
    {
        var googleAuthConfig = configuration.GetSection("Authentication:Google");

        return new GoogleOAuthConfig(
            Validate(googleAuthConfig["ClientId"]),
            Validate(googleAuthConfig["ClientSecret"]),
            Validate(googleAuthConfig["CallbackPath"]));

        static string Validate(string? configProperty) =>
            !string.IsNullOrWhiteSpace(configProperty)
                ? configProperty
                : throw new InvalidOperationException(
                    "Google OAuth is misconfigured. 'Authentication:Google' requires 'ClientId', 'ClientSecret', and 'CallbackPath'.");
    }
}
