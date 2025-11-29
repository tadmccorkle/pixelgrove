using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Security.Claims;
using Csm.PixelGrove.Middleware;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;

const string DefaultScheme = "MultiAuth";
const string CookieScheme = "Cookie";
const string BearerScheme = "Bearer";
const string ApiKeyScheme = "ApiKey";

var builder = WebApplication.CreateBuilder(args);
var isDev = builder.Environment.IsDevelopment();

builder.Services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("App Database"));

builder.Services.AddOpenApi();
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = DefaultScheme;
        options.DefaultChallengeScheme = DefaultScheme;
    })
    .AddCookie(CookieScheme, options =>
    {
        options.Cookie.Name = "auth";
        options.LoginPath = "/login";
        options.LogoutPath = "/auth/logout";
    })
    // TODO(tad): add jwt bearer token auth
    // TODO(tad): add api-key auth
    .AddPolicyScheme(DefaultScheme, "Cookie or Header", options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            if (context.Request.Headers.TryGetValue("Authorization", out var authHeader) && authHeader.Count > 0)
            {
                var authType = authHeader.ToString();
                if (authType.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    // return BearerScheme;
                }

                if (authType.StartsWith("ApiKey ", StringComparison.OrdinalIgnoreCase))
                {
                    // return ApiKeyScheme;
                }
            }

            return CookieScheme;
        };
    })
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
        options.CallbackPath = "/auth/login/callback/google";

        options.Events.OnTicketReceived = async context =>
        {
            var principal = context.Principal!;
            var googleId = principal.FindFirstValue(ClaimTypes.NameIdentifier);

            var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            var account = await db.Accounts.AsNoTracking()
                .Include(account => account.User)
                .FirstOrDefaultAsync(a => a.Provider == "Google" && a.ProviderId == googleId);
            var user = account?.User;

            if (user is null)
            {
                var name = principal.FindFirstValue(ClaimTypes.Name);
                var email = principal.FindFirstValue(ClaimTypes.Email);

                var newUser = db.Users.Add(new()
                {
                    Name = name,
                    Email = email,
                });
                db.Accounts.Add(new()
                {
                    Provider = "Google",
                    ProviderId = googleId,
                    ProviderEmail = email,
                    User = newUser.Entity,
                });

                await db.SaveChangesAsync();

                user = newUser.Entity;
            }

            ClaimsIdentity identity = new([
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email ?? string.Empty),
                new(ClaimTypes.Name, user.Name),
            ], CookieAuthenticationDefaults.AuthenticationScheme);

            principal.AddIdentity(identity);
        };
    });
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("WebOnly", policy => { policy.RequireAuthenticatedUser().AddAuthenticationSchemes(CookieScheme); })
    .AddPolicy("ApiOnly",
        policy => { policy.RequireAuthenticatedUser().AddAuthenticationSchemes(BearerScheme, ApiKeyScheme); });
builder.Services.AddAntiforgery(options => { options.HeaderName = "X-XSRF-TOKEN"; });

if (isDev)
{
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins("http://localhost:4815", "http://localhost:3001")
                .AllowCredentials()
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });

    builder.Services.AddReverseProxy()
        .LoadFromMemory([
            new RouteConfig
            {
                RouteId = "devServer",
                ClusterId = "devCluster",
                Match = new RouteMatch { Path = "{**catchall}" },
            }
        ], [
            new ClusterConfig
            {
                ClusterId = "devCluster",
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    { "default", new DestinationConfig { Address = "http://localhost:3001" } }
                },
                HttpRequest = new ForwarderRequestConfig { ActivityTimeout = TimeSpan.MaxValue },
            }
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
});

app.MapPost("/auth/logout", async context =>
{
    await context.SignOutAsync(CookieScheme);
    context.Response.Redirect("/");
}).RequireAuthorization(policy => policy.RequireAuthenticatedUser().AddAuthenticationSchemes(CookieScheme));

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