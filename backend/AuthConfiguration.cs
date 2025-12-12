using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Csm.PixelGrove;

internal static class AuthConfiguration
{
    public const string DefaultPolicyScheme = "MultiAuth";
    public const string CookiePolicyScheme = "Cookie";
    public const string BearerPolicyScheme = "Bearer";
    public const string ApiKeyPolicyScheme = "ApiKey";

    public const string WebOnlyPolicy = "WebOnly";
    public const string ApiOnlyPolicy = "ApiOnly";

    public const string AuthCookieName = "auth";

    public static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(options =>
            {
                options.DefaultScheme = DefaultPolicyScheme;
                options.DefaultChallengeScheme = DefaultPolicyScheme;
            })
            .AddCookie(CookiePolicyScheme, options =>
            {
                options.Cookie.Name = AuthCookieName;
                options.LoginPath = "/login";
                options.LogoutPath = "/auth/logout";
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
                var authConfig = configuration.GetSection("Authentication");
                var googleAuthConfig = authConfig.GetSection("Google");
                options.ClientId = googleAuthConfig["ClientId"]!;
                options.ClientSecret = googleAuthConfig["ClientSecret"]!;
                options.CallbackPath = "/auth/login/callback/google";
                options.EventsType = typeof(GoogleOAuthEvents);
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(WebOnlyPolicy, policy
                => policy.RequireAuthenticatedUser().AddAuthenticationSchemes(CookiePolicyScheme))
            .AddPolicy(ApiOnlyPolicy, policy
                => policy.RequireAuthenticatedUser().AddAuthenticationSchemes(BearerPolicyScheme, ApiKeyPolicyScheme));

        services.AddAntiforgery(options => options.HeaderName = "X-XSRF-TOKEN");

        return services;
    }
}

internal partial class GoogleOAuthEvents : OAuthEvents
{
    private readonly AppDbContext db;
    private readonly ILogger logger;

    public GoogleOAuthEvents(AppDbContext db, ILogger<GoogleOAuthEvents> logger)
    {
        this.db = db;
        this.logger = logger;
    }

    public override async Task TicketReceived(TicketReceivedContext context)
    {
        context.Fail("TEST AUTH FAILED");
        context.SkipHandler();
        return;
        var principal = context.Principal;
        if (principal == null)
        {
            this.HandleError(context, "Ticket missing claims principal.");
            return;
        }

        var googleId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (googleId == null)
        {
            this.HandleError(context, "Ticket claims principal missing name identifier.");
            return;
        }

        var account = await this.db.Accounts.AsNoTracking()
            .Include(account => account.User)
            .FirstOrDefaultAsync(a => a.Provider == Account.ProviderGoogle && a.ProviderId == googleId);

        var user = account?.User;
        if (user is null)
        {
            var name = principal.FindFirstValue(ClaimTypes.Name);
            var email = principal.FindFirstValue(ClaimTypes.Email);
            if (name == null || email == null)
            {
                this.HandleError(context, "Ticket claims principal missing name and/or email");
                return;
            }

            var newUser = this.db.Users.Add(new User
            {
                Name = name,
                Email = email,
            });
            this.db.Accounts.Add(new Account
            {
                Provider = Account.ProviderGoogle,
                ProviderId = googleId,
                ProviderEmail = email,
                User = newUser.Entity,
            });

            await this.db.SaveChangesAsync();

            user = newUser.Entity;
        }

        ClaimsIdentity identity = new([
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.Name, user.Name),
        ], CookieAuthenticationDefaults.AuthenticationScheme);

        principal.AddIdentity(identity);
    }

    private void HandleError(TicketReceivedContext context, string error)
    {
        this.LogError(error);
        ErrorRedirect(context);
    }

    private static void ErrorRedirect(TicketReceivedContext context)
    {
        context.HandleResponse();
        // TODO(tad): appropriate error redirect
        context.Response.Redirect("/login");
    }

    [LoggerMessage(LogLevel.Error, "{error}")]
    partial void LogError(string error);

    [LoggerMessage(LogLevel.Error, "Unexpected exception occurred when processing claims.")]
    partial void LogException(Exception ex);
}
