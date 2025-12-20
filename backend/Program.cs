using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Csm.PixelGrove.Auth;
using Csm.PixelGrove.Middleware;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;

var builder = WebApplication.CreateBuilder(args);
var isDev = builder.Environment.IsDevelopment();

builder.Services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("App Database"));

builder.Services.AddOpenApi();
builder.Services.AddAuth(builder.Configuration);

if (isDev)
{
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            if (builder.Configuration["Dev:Web:BrowserUrl"] is { } browserUrl) policy.WithOrigins(browserUrl);
            if (builder.Configuration["Dev:Web:ServerUrl"] is { } serverUrl) policy.WithOrigins(serverUrl);
            policy.AllowCredentials().AllowAnyHeader().AllowAnyMethod();
        });
    });

    builder.Services.AddReverseProxy()
        .LoadFromMemory([
            new RouteConfig
            {
                RouteId = "devServer",
                ClusterId = "devCluster",
                Match = new RouteMatch { Path = "{**catchall}" },
            },
        ], [
            new ClusterConfig
            {
                ClusterId = "devCluster",
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    {
                        "default",
                        new DestinationConfig
                        {
                            Address = builder.Configuration["Dev:Web:ServerUrl"] ?? "http://localhost:3001"
                        }
                    },
                },
                HttpRequest = new ForwarderRequestConfig { ActivityTimeout = TimeSpan.MaxValue },
            },
        ]);
}

var app = builder.Build();

if (isDev)
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.UseMiddleware<CsrfRequestTokenCookie>();

if (isDev)
{
    app.UseCors();
    app.MapReverseProxy();
}
else
{
    app.UseFileServer();
    app.MapFallbackToFile("index.html");
}

app.MapGet("/auth/login", (string? provider, string? returnUrl) =>
{
    provider ??= string.Empty;
    var scheme = provider.ToLowerInvariant().AsSpan().Trim() switch
    {
        "google" or _ => GoogleDefaults.AuthenticationScheme,
    };

    var redirectUri = "/";
    if (!string.IsNullOrEmpty(returnUrl) && IsRelativeUri(returnUrl) && !returnUrl.StartsWith("//"))
    {
        redirectUri = returnUrl.StartsWith('/') ? returnUrl : $"/{returnUrl}";
    }

    return Results.Challenge(new AuthenticationProperties { RedirectUri = redirectUri }, [scheme]);

    static bool IsRelativeUri(string url) => Uri.TryCreate(url, UriKind.Relative, out _);
});

app.MapPost("/auth/logout", async (HttpContext context, ILoggerFactory loggerFactory) =>
{
    if (!context.User.TryGetAppUserId(out var userId))
        userId = Guid.Empty;

    await context.SignOutAsync(AuthConfiguration.CookiePolicyScheme);

    loggerFactory
        .CreateLogger("Auth")
        .LogInformation("User {UserId} logged out", userId);

    context.Response.Redirect("/");
}).AddEndpointFilter<IEndpointConventionBuilder, CsrfEndpointFilter>().RequireAuthorization();

app.MapGet("/api/users/{id}", async (HttpContext context, AppDbContext db, string id) =>
{
    if (id == "me")
    {
        if (!(context.User.Identity?.IsAuthenticated ?? false))
        {
            return Results.Unauthorized();
        }

        if (!context.User.TryGetAppUserId(out var userId))
        {
            return Results.BadRequest("Authenticated user has invalid or missing id claim.");
        }

        var user = await db.Users.FindAsync(userId);
        return user is not null
            ? Results.Ok(user)
            : Results.NotFound();
    }

    if (Guid.TryParse(id, out _))
    {
        return Results.StatusCode(StatusCodes.Status501NotImplemented);
    }

    return Results.BadRequest("Invalid user id.");
});

app.Run();

internal class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Account> Accounts { get; set; }
}

internal class User
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Email { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTime CreatedAt { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public DateTime UpdatedAt { get; set; }

    public ICollection<Account> Accounts { get; set; }
}

internal class Account
{
    public const string ProviderGoogle = "Google";

    public Guid Id { get; set; }
    public required string Provider { get; set; }
    public required string ProviderId { get; set; }
    public string? ProviderEmail { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTime CreatedAt { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public DateTime UpdatedAt { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; }
}
