// Copyright (c) 2026 by Tad McCorkle
// Licensed under the MIT license.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Csm.PixelGrove.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Csm.PixelGrove.Auth;

internal sealed partial class GoogleOAuthEvents : OAuthEvents
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
        try
        {
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

            if (account is not null && user is null)
            {
                this.HandleError(context, $"Existing account {account.Id} has no associated user.");
                return;
            }

            if (user is null)
            {
                var name = principal.FindFirstValue(ClaimTypes.Name);
                var email = principal.FindFirstValue(ClaimTypes.Email);
                if (name == null || email == null)
                {
                    this.HandleError(context, "Ticket claims principal missing name and/or email.");
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

            this.LogUserLoggedIn(user.Id);
        }
        catch (Exception ex)
        {
            this.LogException(ex, context.HttpContext.Request.Path.ToString());
            HandleErrorResponse(context, ExceptionDescription);
        }
    }

    private void HandleError(TicketReceivedContext context, string error)
    {
        this.LogError(error, context.HttpContext.Request.Path.ToString());
        HandleErrorResponse(context, error);
    }

    private static void HandleErrorResponse(TicketReceivedContext context, string error)
    {
        context.Fail(error);
        context.HandleResponse();
        context.Response.Redirect("/login?error=auth");
    }

    [LoggerMessage(LogLevel.Information, $"User {{UserId}} logged in via {Account.ProviderGoogle}.")]
    partial void LogUserLoggedIn(Guid userId);

    [LoggerMessage(LogLevel.Error, "Google OAuth error: {Error}. Path: {Path}")]
    partial void LogError(string error, string path);

    private const string ExceptionDescription = "Unexpected exception occurred when processing Google OAuth ticket.";

    [LoggerMessage(LogLevel.Error, $"{ExceptionDescription} Path: {{Path}}")]
    partial void LogException(Exception ex, string path);
}
