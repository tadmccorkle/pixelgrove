using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Claims;
using Csm.PixelGrove;
using Csm.PixelGrove.Middleware;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;

var builder = WebApplication.CreateBuilder(args);
var isDev = builder.Environment.IsDevelopment();

builder.Services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("App Database"));
builder.Services.AddScoped<GoogleOAuthEvents>();

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

    return Results.Challenge(new() { RedirectUri = returnUrl ?? "/", }, [scheme]);
}).AddEndpointFilter<CsrfEndpointFilter>();

app.MapPost("/auth/logout", async context =>
{
    await context.SignOutAsync(AuthConfiguration.CookiePolicyScheme);
    context.Response.Redirect("/");
}).RequireAuthorization(policy
    => policy.RequireAuthenticatedUser().AddAuthenticationSchemes(AuthConfiguration.CookiePolicyScheme));

app.MapGet("/api/users/{id}", async (HttpContext context, AppDbContext db, string id) =>
    {
        if (id == "me")
        {
            if (!(context.User.Identity?.IsAuthenticated ?? false))
            {
                return Results.Unauthorized();
            }

            var userIdValue = context.User
                .FindFirst(c => c is { Type: ClaimTypes.NameIdentifier, Issuer: ClaimsIdentity.DefaultIssuer })?.Value;
            if (userIdValue is null || !Guid.TryParse(userIdValue, out var userId))
            {
                return Results.BadRequest("");
            }

            var user = await db.Users.FindAsync(userId);
            return user is not null
                ? Results.Ok(user)
                : Results.NotFound();
        }

        if (Guid.TryParse(id, out var _))
        {
            // TODO(tad): support getting other user info
            return Results.Unauthorized();
        }

        return Results.BadRequest("Invalid user id.");
    })
    .RequireAuthorization()
    .AddEndpointFilter<CsrfEndpointFilter>();

app.MapGet("/weatherforecast", () =>
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55)
                ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast")
    .RequireAuthorization()
    .AddEndpointFilter<CsrfEndpointFilter>();

app.MapGet("/test", async (HttpContext context, AppDbContext db) =>
{
    // return Results.Ok(
    //     new { id = context.User.Identity, user = context.User.Claims.Select(c => $"{c.Type}: {c.Value} ({c.Issuer})").ToArray() });
    // var tokens = af.GetAndStoreTokens(context);
    // return Results.Ok(new
    // {
    //     token = tokens.RequestToken,
    //     cookie = tokens.CookieToken,
    //     headerName = tokens.HeaderName,
    // });
});

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public WeatherForecast(DateOnly Date, int TemperatureC)
        : this(Date, TemperatureC, NextSummary)
    {
    }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    private static string NextSummary => Summaries[Random.Shared.Next(Summaries.Length)];

    private static readonly string[] Summaries =
    [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];
}

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
